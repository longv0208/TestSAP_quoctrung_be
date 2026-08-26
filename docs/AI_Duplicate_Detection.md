# Rà trùng lặp đề cương bằng AI

> Trả lời gạch 3 của biên bản hội đồng bảo vệ lần 2: *"cân nhắc bổ sung tính năng AI kiểm tra trùng
> proposal"*. Tài liệu này là phần **hội đồng thực sự chấm** ở hạng mục AI — không phải mã nguồn,
> mà là: giải quyết vấn đề gì, đo bằng gì, ngưỡng ở đâu ra, ai chịu trách nhiệm cho đầu ra.

---

## 1. Vấn đề

Phòng QLKH nhận hàng chục đề cương mỗi đợt. Trước đây **không có cách nào** biết một đề cương mới
có trùng với đề tài đã cấp kinh phí các năm trước hay không, ngoài việc nhớ. Hệ quả: hai đề tài
cùng nội dung có thể cùng được duyệt ở hai đợt khác nhau.

Đây **không phải** bài toán tìm câu văn bị chép. Đề cương trùng thường được **viết lại hoàn toàn**:
cùng mục tiêu, cùng phương pháp, cùng sản phẩm, nhưng không chung một câu nào. So khớp chuỗi ký tự
bỏ sót đúng nhóm này — nên phải so **ngữ nghĩa**.

> ⚠️ **Đừng nhầm với FE-08 trong tài liệu cũ.** Hai thứ ngược chiều nhau:
>
> | | FE-08 (tài liệu cũ mô tả) | Tính năng này |
> |---|---|---|
> | So gì | đề cương ↔ **đơn đặt hàng** | đề cương ↔ **kho đề tài đã có** |
> | Cảnh báo khi | điểm **THẤP** (không bám đặt hàng) | điểm **CAO** (nghi trùng) |

---

## 2. Kiến trúc hai tầng

```
Đề cương nộp
     │
     ├─► TẦNG 1  vector hoá (gemini-embedding-001) → cosine với cả kho → top-K
     │            rẻ · chạy mọi lần · TẤT ĐỊNH ⇒ đo được Precision/Recall
     │
     ├─► TẦNG 2  chỉ khi có cặp vượt ngưỡng, hoặc Phòng QLKH bấm
     │            gọi mô hình sinh chữ → viết ra GIỐNG Ở CHỖ NÀO
     │            cache vào llm_outputs ⇒ mở lại lần hai không tốn quota
     │
     └─► NGƯỜI   Phòng QLKH chốt kết luận → ghi vào sổ quyết định của đề tài
```

**Vì sao tách hai tầng thay vì để mô hình sinh chữ tự chấm điểm giống nhau:** mô hình sinh chữ cho
kết quả **khác nhau giữa hai lần chạy cùng một cặp**. Không tất định thì không có Precision/Recall
nào bảo vệ được, và cũng không có ngưỡng nào ổn định. Cosine trên vector nhúng luôn ra cùng một số.

**Vì sao không dùng pgvector:** ảnh `postgres:16` chuẩn không kèm extension. `CREATE EXTENSION
vector` mà fail thì `Migrate()` lúc khởi động làm **app không lên nổi** trên bản deploy đang có dữ
liệu thật. Kho cỡ vài trăm bản ghi thì quét tuần tự trong bộ nhớ dưới một giây — lợi ích của chỉ mục
vector bằng không, còn rủi ro thì không. Vector lưu dạng JSON trong cột `text`; 1000 đề cương ≈ 12 MB.

---

## 3. Dữ liệu đem vector hoá

Ghép **tên + tóm tắt + mục tiêu + sản phẩm dự kiến**.

Chỉ lấy mỗi tên thì hai đề tài đặt tên na ná nhau đã báo động, còn hai đề tài trùng nội dung mà đặt
tên khác thì lọt. Bộ đo cũng ghép **đúng công thức này** — đo trên thứ khác với thứ chạy thật thì
con số metric không nói lên điều gì về hệ thống.

**Khống chế quota:** cột `content_hash` (vốn đã có sẵn trong bảng) so nội dung; không đổi thì bỏ
qua. Chạy lại lệnh lập chỉ mục mười lần cũng chỉ tốn cho những bản thật sự mới.

---

## 4. Bộ gán nhãn

`FURPMS.Tests/Ai/duplicate-eval-set.json` — **28 văn bản, 35 cặp**, hai thành viên gán độc lập rồi
thảo luận chốt.

| Loại | Số cặp | Là gì |
|---|---:|---|
| `PARAPHRASE` | 10 | **Trùng thật**: cùng mục tiêu + cùng phương pháp + cùng sản phẩm, chỉ khác câu chữ |
| `HARD_NEGATIVE` | 10 | **Cùng lĩnh vực, khác đề tài** — chỗ mô hình dễ sai nhất |
| `EASY_NEGATIVE` | 15 | Khác lĩnh vực hoàn toàn |

**Nhóm `HARD_NEGATIVE` là phần đắt nhất.** Thiếu nó thì mọi ngưỡng đều trông đẹp: chỉ cần phân biệt
"cùng ngành" với "khác ngành" là đủ ăn điểm cao, mà đó không phải bài toán thật. Ví dụ:

- *Dự báo* bỏ học bằng học máy **vs** *phân tích yếu tố* ảnh hưởng kết quả học tập bằng thống kê
- Nhận dạng **chữ viết tay** tiếng Việt **vs** nhận dạng **tiếng nói** tiếng Việt
- Xếp lịch **thi** **vs** phân bổ **phòng học** — cùng tối ưu hoá, khác bài toán

---

## 5. Kết quả đo

Chạy `dotnet test --filter DuplicateThresholdEvaluationTests`. Vector nằm sẵn trong file (sinh một
lần bằng model thật) nên **quét ngưỡng chạy hoàn toàn offline**, cho cùng một con số giữa hai lần chạy.

Model `gemini-embedding-001` · 35 cặp · đo ngày 26/08/2026:

| Ngưỡng | TP | FP | FN | TN | Precision | Recall |    F1 |    F2 |
|-------:|---:|---:|---:|---:|----------:|-------:|------:|------:|
|   0.70 | 10 | 10 |  0 | 15 |     0.500 |  1.000 | 0.667 | 0.833 |
|   0.74 | 10 |  8 |  0 | 17 |     0.556 |  1.000 | 0.714 | 0.862 |
|   0.78 | 10 |  3 |  0 | 22 |     0.769 |  1.000 | 0.870 | 0.943 |
|   0.82 | 10 |  1 |  0 | 24 |     0.909 |  1.000 | 0.952 | 0.980 |
| **0.84** | 10 |  0 |  0 | 25 | **1.000** | **1.000** | **1.000** | **1.000** |
| **0.86** | 10 |  0 |  0 | 25 | **1.000** | **1.000** | **1.000** | **1.000** |
| **0.88** | 10 |  0 |  0 | 25 | **1.000** | **1.000** | **1.000** | **1.000** |
|   0.90 |  6 |  0 |  4 | 25 |     1.000 |  0.600 | 0.750 | 0.652 |

Hai nhóm tách rời nhau:

| | Trung bình | Biên |
|---|---:|---|
| Cặp trùng thật | 0.913 | thấp nhất **0.889** |
| Cặp cùng lĩnh vực khác đề tài | 0.775 | cao nhất **0.835** |

### Vì sao chọn 0.86

Khoảng trống giữa hai nhóm là **[0.835 – 0.889]**. Chọn **0.86** là **giữa khoảng trống đó**, chừa
biên độ cho cả hai phía thay vì bám sát mép một nhóm. Lấy 0.88 (điểm F2 cao nhất theo máy) thì sát
ngay dưới cặp trùng thấp nhất 0.889 — một chút nhiễu là mất recall.

### ⚠️ Giới hạn của phép đo này — phải nói ra trước khi hội đồng hỏi

- **35 cặp là ít.** Đủ để chọn ngưỡng, không đủ để tuyên bố độ chính xác của hệ thống.
- **Do chính nhóm soạn.** Các cặp trùng là paraphrase nhóm tự viết, nên "sạch" hơn đề cương trùng
  ngoài đời — người viết thật sẽ đổi cả cấu trúc, không chỉ đổi từ.
- ⇒ **F1 = 1.000 KHÔNG có nghĩa hệ thống không bao giờ sai.** Nó nói: trên bộ này, tồn tại một
  khoảng ngưỡng tách được hai nhóm, và 0.86 nằm giữa khoảng đó.
- Ngưỡng để trong `system_settings`, Admin sửa được **không cần khởi động lại** — vì con số này
  chắc chắn phải chỉnh khi có dữ liệu thật.

---

## 6. Người trong vòng lặp

Hệ thống **không bao giờ tự loại một đề tài**. Ba mức chỉ để xếp thứ tự xem:

| Mức | Nghĩa |
|---|---|
| `LOW` | dưới ngưỡng — liệt kê cho đủ ngữ cảnh |
| `WARN` | ≥ `AI_DUPLICATE_THRESHOLD` — Phòng QLKH nên xem |
| `HIGH` | ≥ `AI_DUPLICATE_BLOCK_THRESHOLD` — xem trước tiên. **Vẫn không tự chặn nộp** |

Phòng QLKH chốt một trong ba kết luận, và **kết luận có vấn đề thì bắt buộc ghi căn cứ** (chủ nhiệm
cần biết phải sửa gì):

`NOT_DUPLICATE` · `NEEDS_REVISION` · `DUPLICATE`

Kết luận sinh một dòng `DUPLICATE_REVIEWED` trong **sổ quyết định của đề tài** (xem
`API_CONTRACT.md` §13.2g), kèm tên và chức danh người quyết định. Về sau luôn tra được: cảnh báo
này đã có ai xem chưa, và họ kết luận ra sao.

---

## 7. Quản lý chi phí

Mỗi lần chạy tầng 2 ghi vào `llm_outputs`: `tokens_input`, `tokens_output`, `latency_ms`, `model_used`.
Ba cột này có trong bảng từ đầu nhưng **chưa luồng nào ghi** — không ghi thì không trả lời được câu
*"nhóm có quản lý chi phí AI không"*.

Đo thật trên một lần chạy (26/08): **463 token vào / 296 token ra / 1.897 ms**.

Ba cơ chế giữ quota:

1. **Tầng 1 không tốn gì** sau khi đã vector hoá — cosine chạy trong bộ nhớ.
2. **Không vector hoá lại** khi nội dung không đổi (so `content_hash`).
3. **Tầng 2 cache theo đề cương** — mở lại hồ sơ lần thứ hai trả bản đã lưu; muốn gọi lại phải bấm
   "giải thích lại" một cách có chủ đích.

---

## 8. Cấu hình

| Khoá | Mặc định | Ý nghĩa |
|---|---:|---|
| `AI_DUPLICATE_THRESHOLD` | 0.86 | Từ mức này trở lên thì cảnh báo |
| `AI_DUPLICATE_TOP_K` | 5 | Lấy bao nhiêu đề tài giống nhất để đối chiếu |
| `AI_DUPLICATE_BLOCK_THRESHOLD` | 0.92 | Từ mức này đánh dấu "gần như trùng khít" |
| `GeminiAI:EmbeddingModel` | `gemini-embedding-001` | Model nhúng |

> **Ghi chú về model:** tài liệu RP1/RP3/RP7 ghi `text-embedding-004`. Hỏi `ListModels` bằng chính
> khoá của nhóm ngày 26/08 thì model đó **không còn phục vụ `embedContent`** nữa; khoá này có
> `gemini-embedding-001`. **Tài liệu phải sửa theo cái đang chạy**, không phải ngược lại.

---

## 9. Cách chạy lại phép đo

```bash
# 1. Vector hoá bộ gán nhãn bằng model thật (chạy 1 lần, cần GeminiAI:ApiKey)
#    BE ở Development, đăng nhập tài khoản Quản trị
POST /api/admin/embed-eval-set

# 2. Quét ngưỡng — offline, in ra bảng P/R/F1/F2
dotnet test --filter DuplicateThresholdEvaluationTests -l "console;verbosity=detailed"

# 3. Vector hoá kho đề cương thật
POST /api/admin/reindex-embeddings?max=200
```

Bộ test bắt luôn hai thứ dễ hỏng âm thầm: bộ gán nhãn **tự mâu thuẫn** (một cặp gán hai nhãn), và
hai nhóm **chồng lên nhau** (không tồn tại ngưỡng nào dùng được).

---

## 10. Việc còn để ngỏ

- **Chưa có ngưỡng riêng theo lĩnh vực.** Đề tài trong cùng một ngành hẹp tự nhiên giống nhau hơn.
  Có dữ liệu thật rồi mới chỉnh được, và chỉnh bằng cách sửa `system_settings` chứ không sửa mã.
- **Chưa so với đề tài của trường khác** — kho hiện chỉ có đề tài trong hệ thống.
- **Bộ gán nhãn nên mở rộng** bằng chính các ca thật Phòng QLKH gặp: mỗi lần họ chốt
  `DUPLICATE`/`NOT_DUPLICATE` là một nhãn do người thật gán, đáng giá hơn nhiều so với cặp tự soạn.
