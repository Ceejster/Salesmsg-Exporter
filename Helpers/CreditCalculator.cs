using SalesmessageExporter.Models;

namespace SalesmessageExporter.Helpers
{
    public static class CreditCalculator
    {
        // GSM-7 basic character set
        private static readonly HashSet<char> Gsm7Basic = new()
        {
            '@','£','$','¥','è','é','ù','ì','ò','Ç','\n','Ø','ø','\r','Å','å',
            'Δ','_','Φ','Γ','Λ','Ω','Π','Ψ','Σ','Θ','Ξ','Æ','æ','ß','É',
            ' ','!','"','#','¤','%','&','\'','(',')','*','+',',','-','.','/',
            '0','1','2','3','4','5','6','7','8','9',':',';','<','=','>','?',
            '¡','A','B','C','D','E','F','G','H','I','J','K','L','M','N','O',
            'P','Q','R','S','T','U','V','W','X','Y','Z','Ä','Ö','Ñ','Ü','§',
            '¿','a','b','c','d','e','f','g','h','i','j','k','l','m','n','o',
            'p','q','r','s','t','u','v','w','x','y','z','ä','ö','ñ','ü','à'
        };

        // GSM-7 extended characters — each counts as 2 chars (escape + char)
        private static readonly HashSet<char> Gsm7Extended = new()
        {
            '\f', '|', '^', '€', '{', '}', '[', ']', '~', '\\'
        };

        // Returns true if the entire string fits in the GSM-7 charset
        public static bool IsGsm7(string text)
        {
            foreach (char c in text)
                if (!Gsm7Basic.Contains(c) && !Gsm7Extended.Contains(c))
                    return false;

            return true;
        }

        // Returns the encoded byte length of a GSM-7 string
        // (extended chars each consume 2 of the 160-char budget)
        public static int GetGsm7Length(string text)
        {
            int len = 0;

            foreach (char c in text)
                len += Gsm7Extended.Contains(c) ? 2 : 1;

            return len;
        }

        // Number of SMS segments a body occupies
        //   GSM-7 : single=160 chars, multipart=153 chars/segment
        //   Unicode: single=70  chars, multipart=67  chars/segment
        public static int CalculateSmsSegments(string body)
        {
            if (string.IsNullOrEmpty(body))
                return 0;

            if (IsGsm7(body))
            {
                int len = GetGsm7Length(body);
                return len <= 160 ? 1 : (int)Math.Ceiling((double)len / 153);
            }
            else
            {
                int len = body.Length;
                return len <= 70 ? 1 : (int)Math.Ceiling((double)len / 67);
            }
        }

        // Credits consumed by a single message:
        //   SMS  → number of segments
        //   MMS  → 1 flat
        //   call / other → 0 (different billing)
        // Returns 0 for inbound messages (status == "received")
        // Credits consumed by a call: 1 per started minute (ceiling)
        public static int CalculateCallCredits(int durationSeconds)
        {
            if (durationSeconds <= 0)
                return 0;

            return (int)Math.Ceiling(durationSeconds / 60.0);
        }

        // Credits consumed by a single message:
        //   SMS  → number of segments (1 credit each)
        //   MMS  → 2 flat
        //   call → ceil(seconds / 60)
        // Returns 0 for inbound or failed messages
        public static int CalculateCredits(ExportMessage message)
        {
            if (message == null)
                return 0;

            if (string.Equals(
                    message.Status, "received",
                    StringComparison.OrdinalIgnoreCase))
                return 0;

            if (string.Equals(
                    message.Status, "failed",
                    StringComparison.OrdinalIgnoreCase))
                return 0;

            string type = message.Type?.ToLower() ?? "";

            return type switch
            {
                "mms"  => 2,
                "sms"  => CalculateSmsSegments(message.Body ?? ""),
                "call" => CalculateCallCredits(message.Record?.Duration ?? 0),
                _      => 0
            };
        }
    }
}
