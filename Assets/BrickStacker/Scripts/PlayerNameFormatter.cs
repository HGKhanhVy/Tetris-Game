using System.Text;

namespace BrickStacker
{
    // Chuẩn hoá tên người chơi cho vừa luật của Unity Authentication.
    // Đã kiểm chứng trực tiếp với dịch vụ: tên GIỮ NGUYÊN DẤU tiếng Việt (Bảo_Ngọc, Nguyễn-Văn.A
    // đều lưu được), server chỉ từ chối KHOẢNG TRẮNG ("Player names cannot be empty or contain
    // spaces"). Khoảng trắng KHÔNG bị tự đổi thành '_' — giao diện báo cho người chơi tự sửa
    // (xem HasSpace), vì tự ý sửa tên của người ta là chuyện không nên làm âm thầm.
    public static class PlayerNameFormatter
    {
        public const int MinLength = 2;
        public const int MaxLength = 12;   // vừa cột NGƯỜI CHƠI của bảng xếp hạng

        public static string Sanitize(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "";

            var builder = new StringBuilder(raw.Length);
            foreach (char c in raw.Trim())
            {
                if (IsAllowed(c))
                    builder.Append(c);

                if (builder.Length >= MaxLength)
                    break;
            }

            return builder.ToString().Trim('_', '.', '-');
        }

        // Tên còn khoảng trắng ở giữa (đã bỏ qua khoảng trắng thừa hai đầu) -> phải báo người chơi.
        public static bool HasSpace(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return false;

            foreach (char c in raw.Trim())
            {
                if (char.IsWhiteSpace(c))
                    return true;
            }
            return false;
        }

        public static bool IsValid(string sanitized)
        {
            return !string.IsNullOrEmpty(sanitized) && sanitized.Length >= MinLength;
        }

        // Chữ/số theo Unicode (nên chữ tiếng Việt có dấu được giữ) cộng 3 ký tự nối thông dụng.
        // Emoji và ký hiệu lạ bị loại vì server không nhận.
        static bool IsAllowed(char c)
        {
            return char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-';
        }
    }
}
