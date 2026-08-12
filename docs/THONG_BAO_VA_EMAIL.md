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
| `PROPOSAL_SUBMITTED` | Chủ nhiệm **nộp đề cương** | Toàn bộ Phòng QLKH | ✅ | ✅ | NORMAL |
| `PROPOSAL_RESUBMITTED` | Chủ nhiệm **nộp lại** sau khi chỉnh sửa (bản v2+) | Toàn bộ Phòng QLKH | ✅ | ✅ | NORMAL |
| `CONTRACT_SIGNED` | Hợp đồng được **ký** | Chủ nhiệm | ✅ | ✅ | HIGH |
| `MINUTES_FINALIZED` | Chủ tịch **duyệt & khoá biên bản** | **Toàn bộ thành viên hội đồng** | ✅ | ✅ | NORMAL |
| `ACCEPTANCE_FINALIZED` | Khoá biên bản của vòng **nghiệm thu** | Chủ nhiệm | ✅ | ✅ | HIGH |
| `CYCLE_DEADLINE_EXTENDED` | **Gia hạn hạn nộp** của đợt | Mọi chủ nhiệm **có đề tài trong đợt đó** | ✅ | ✅ | HIGH |
| `ACCOUNT_CREATED` | Admin **tạo tài khoản mới** | Chính người được tạo | ✅ | ✅ | HIGH |

**15 loại**, chia hai nhóm:

- **Theo sự kiện** (11 loại) — bắn ngay lúc ai đó bấm nút.
- **Theo lịch quét** (4 loại nhắc hạn/quá hạn) — `DeadlineReminderService` là background service,
  **chạy mỗi 24 giờ**, và **chống gửi trùng**: đã bắn `DEADLINE_REMINDER_T3` cho sản phẩm đó rồi thì
  lần quét sau không bắn lại. Admin có nút chạy tay để demo (không phải đợi 24h).

> **Vì sao vòng xét duyệt có `REVIEW_ROUND_CLOSED` mà không có `ACCEPTANCE_FINALIZED`, còn vòng
> nghiệm thu thì ngược lại?** Chủ nhiệm chỉ nên nhận **một** tin cho một kết quả. Vòng xét duyệt báo
> lúc *đóng vòng*; vòng nghiệm thu không đi qua bước đó nên báo lúc *khoá biên bản*. Thành viên hội
> đồng thì luôn nhận `MINUTES_FINALIZED` ở cả hai loại vòng.

### Muốn demo thông báo ngay lập tức
Đăng nhập Admin → **Dev-tools** → chạy tay bộ quét hạn (`AdminController` gọi
`IDeadlineReminderScanner`). Không cần tua thời gian hệ thống.

---

## 3. Cái CHƯA có — cần biết trước khi hứa với hội đồng

| Thiếu | Ảnh hưởng | Ghi chú |
|---|---|---|
| **Quên mật khẩu qua email** | Người dùng mất mật khẩu phải nhờ Admin đặt lại | `AuthController` hiện chỉ có `login`, `me`, `change-password`. **Không có** `forgot-password`/`reset-password`; giao diện đăng nhập cũng không có liên kết "Quên mật khẩu". Admin có `POST /users/{id}/reset-password`. |
| **Chuông không tự cập nhật** | Phải tải lại trang mới thấy thông báo mới | Chưa có polling / SignalR |
| Thông báo khi **giải ngân được xác nhận** | Chủ nhiệm không biết đợt nào đã chi | Phải tự vào xem tiến trình |
| Thông báo khi **báo cáo tiến độ được duyệt** | Chủ nhiệm không biết kỳ báo cáo đã qua | |
| Quản lý **token/chi phí AI** và **đo chất lượng đầu ra AI** | — | Cẩm nang capstone có nêu; chưa làm |

> ✅ **Đã bổ sung 12/08:** nộp đề cương · ký hợp đồng · khoá biên bản · gia hạn đợt — 6 loại mới,
> đã kiểm end-to-end trên hệ thống thật (xem §4).
>
> Hai dòng còn lại cũng chỉ là **thêm một lời gọi `NotifyAsync` vào chỗ đã có sẵn**, không phải hạ
> tầng mới.

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

### Đã kiểm end-to-end 12/08 (tắt `EMAIL_ENABLED` để không gửi mail thật)

| Thao tác | Kết quả đo được |
|---|---|
| Chủ nhiệm nộp đề cương | `PROPOSAL_SUBMITTED` → Chuyên viên, nội dung nêu **tên người nộp + tên đề tài** |
| Ký hợp đồng | `CONTRACT_SIGNED` → đúng chủ nhiệm của đề tài đó, kèm thời gian thực hiện |
| Chủ tịch khoá biên bản nghiệm thu | `MINUTES_FINALIZED` → **cả 5 thành viên hội đồng**, kèm kết luận; `ACCEPTANCE_FINALIZED` → chủ nhiệm |
| Gia hạn hạn nộp của đợt | `CYCLE_DEADLINE_EXTENDED` → **2 chủ nhiệm trong đợt đó**, nêu hạn cũ → hạn mới + lý do; chủ nhiệm đợt khác **không** nhận |
| `EMAIL_ENABLED = false` | `email_logs` ghi `SKIPPED`, chuông vẫn đủ — đúng thiết kế |

> ⚠️ **Khi test đừng để `EMAIL_ENABLED` bật với địa chỉ thật** — hệ thống gửi mail thật ra ngoài.

---

## 5. Liên quan

- Ràng buộc trên màn hình: `RANG_BUOC_TREN_MAN_HINH.md`
- Bảng kiểm thử: `TEST_CHECKLIST.md`
- Quy tắc nghiệp vụ: `BUSINESS_RULES.md`
