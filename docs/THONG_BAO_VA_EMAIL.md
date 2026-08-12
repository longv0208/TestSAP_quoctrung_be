# Thông báo & Email — hiện trạng

> Trả lời gọn: **có bao nhiêu loại thông báo, kích hoạt khi nào, gửi cho ai, có kèm mail không**,
> và **cái gì chưa có**. Rà bằng cách đọc thẳng mã nguồn + đối chiếu bảng `notifications`,
> `email_logs` trong DB — 12/08/2026.

---

## 1. Cơ chế

Mọi thông báo đi qua **`INotifier`** (`FURPMS.Infrastructure/Services/Notifier.cs`):

```
NotifyAsync(...)  →  ① LUÔN tạo bản ghi `notifications` (chuông trong app)
                  →  ② mặc định gửi kèm EMAIL cùng nội dung  (alsoEmail = true)
```

- Email chịu **công tắc tổng của Admin**: `system_settings.EMAIL_ENABLED`. Tắt thì `email_logs`
  ghi `SKIPPED`, **chuông vẫn chạy bình thường** — tắt mail không làm mất thông báo.
- SMTP cấu hình ở `appsettings.json → EmailSettings` (đang dùng **Brevo relay**). Mật khẩu để ở
  `appsettings.Development.json` (gitignore).
- Mọi lần gửi đều ghi `email_logs` (SENT / FAILED / SKIPPED) — có dấu vết để đối chất.

> ⚠️ **Đã đối chiếu DB thật:** `email_logs` có bản ghi `SENT` ⇒ đường gửi mail **chạy được**, không
> phải mã chết. Mail vào Spam vì `FromEmail` là địa chỉ @gmail gửi qua relay — hạn chế hạ tầng, muốn
> sạch phải có tên miền riêng đã xác thực SPF/DKIM.

---

## 2. Các loại thông báo hiện có

| Mã loại | Kích hoạt khi nào | Gửi cho ai | Chuông | Mail | Mức |
|---|---|---|---|---|---|
| `COUNCIL_INVITATION` | Chuyên viên bấm **"Gửi thư mời"** cho hội đồng (gate: đủ Chủ tịch + Thư ký + lịch họp) | Từng phản biện được mời | ✅ | ✅ | HIGH |
| `REVIEW_ROUND_CLOSED` | **Đóng vòng xét duyệt** — dù kết quả Duyệt / Từ chối / Cần chỉnh sửa | Chủ nhiệm đề tài | ✅ | ✅ | HIGH nếu Từ chối |
| `DELIVERABLE_PASSED` | Sản phẩm được đánh giá **Đạt** | Chủ nhiệm | ✅ | ✅ | NORMAL |
| `DELIVERABLE_FAILED` | Sản phẩm **Không đạt** | Chủ nhiệm **+ toàn bộ Chuyên viên** | ✅ | ✅ | NORMAL |
| `DEADLINE_REMINDER_T{n}` | Sản phẩm còn **n ngày** tới hạn (n lấy từ cấu hình, mặc định có mốc **T-3**) | Chủ nhiệm | ✅ | ✅ | NORMAL |
| `DEADLINE_OVERDUE` | Sản phẩm **quá hạn** | Chủ nhiệm | ✅ | ✅ | HIGH |
| `REPORT_REMINDER_T{n}` | Báo cáo tiến độ còn **n ngày** tới hạn | Chủ nhiệm | ✅ | ✅ | NORMAL |
| `REPORT_OVERDUE` | Báo cáo tiến độ **quá hạn** | Chủ nhiệm | ✅ | ✅ | HIGH |

**8 loại**, chia hai nhóm:

- **Theo sự kiện** (4 loại đầu) — bắn ngay lúc ai đó bấm nút.
- **Theo lịch quét** (4 loại sau) — `DeadlineReminderService` là background service, **chạy mỗi 24
  giờ**, và **chống gửi trùng**: đã bắn `DEADLINE_REMINDER_T3` cho sản phẩm đó rồi thì lần quét sau
  không bắn lại. Admin có nút chạy tay để demo (không phải đợi 24h).

### Muốn demo thông báo ngay lập tức
Đăng nhập Admin → **Dev-tools** → chạy tay bộ quét hạn (`AdminController` gọi
`IDeadlineReminderScanner`). Không cần tua thời gian hệ thống.

---

## 3. Cái CHƯA có — cần biết trước khi hứa với hội đồng

| Thiếu | Ảnh hưởng | Ghi chú |
|---|---|---|
| **Quên mật khẩu qua email** | Người dùng mất mật khẩu phải nhờ Admin đặt lại | `AuthController` hiện chỉ có `login`, `me`, `change-password`. **Không có** `forgot-password`/`reset-password`; giao diện đăng nhập cũng không có liên kết "Quên mật khẩu". Admin có `POST /users/{id}/reset-password`. |
| **Chuông không tự cập nhật** | Phải tải lại trang mới thấy thông báo mới | Chưa có polling / SignalR |
| Thông báo khi **nộp đề cương** | Chuyên viên không được báo có đề cương mới | Phải tự vào màn danh sách xem |
| Thông báo khi **hợp đồng được ký** / **giải ngân được xác nhận** | Chủ nhiệm không biết | Phải tự vào xem |
| Thông báo khi **biên bản được Chủ tịch khoá** | Thành viên hội đồng không biết đã chốt | |
| Thông báo **gia hạn deadline của đợt** | Chủ nhiệm không biết hạn đã đổi | |
| Quản lý **token/chi phí AI** và **đo chất lượng đầu ra AI** | — | Cẩm nang capstone có nêu; chưa làm |

> Bốn dòng giữa (nộp đề cương · ký hợp đồng · khoá biên bản · gia hạn) đều là **thêm một lời gọi
> `NotifyAsync` vào chỗ đã có sẵn**, không phải hạ tầng mới. Rẻ, nhưng chưa làm.

---

## 4. Cách tự kiểm

```bash
# Thông báo nào đã sinh
SELECT notification_type, COUNT(*) FROM notifications GROUP BY notification_type;

# Mail đã gửi / bị bỏ qua / lỗi
SELECT status, COUNT(*) FROM email_logs GROUP BY status;

# Công tắc mail
SELECT [key], value FROM system_settings WHERE [key] = 'EMAIL_ENABLED';
```

Trên giao diện: biểu tượng **chuông** ở thanh trên cùng (mọi vai) → mở màn **Thông báo**.

> ⚠️ **Khi test đừng để `EMAIL_ENABLED` bật với địa chỉ thật** — hệ thống gửi mail thật ra ngoài.

---

## 5. Liên quan

- Ràng buộc trên màn hình: `RANG_BUOC_TREN_MAN_HINH.md`
- Bảng kiểm thử: `TEST_CHECKLIST.md`
- Quy tắc nghiệp vụ: `BUSINESS_RULES.md`
