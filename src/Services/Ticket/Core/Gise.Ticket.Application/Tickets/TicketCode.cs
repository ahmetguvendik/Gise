using System.Security.Cryptography;
using System.Text;

namespace Gise.Ticket.Application.Tickets;

// Bilet kodu: 16 karakter Crockford base32, 80 bit rastgele; tahmin edilemez. QR'ın içeriği budur. Kapıda okuyucu ya da
// elle girilir: elle girişte tire, boşluk, küçük harf ve karıştırılan harfler (O → 0, I ve L → 1) tolere edilir.
public static class TicketCode
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    private const int Length = 16;

    public static string Generate() => new(RandomNumberGenerator.GetItems<char>(Alphabet, Length));

    // "abcd-efgh ijkl mnop" → "ABCDEFGH1JK1MN0P"; bilet kodu olamayacak girişte null.
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var code = new StringBuilder(Length);
        foreach (var c in input.ToUpperInvariant())
        {
            if (c is '-' or ' ')
                continue;

            var mapped = c switch
            {
                'O' => '0',
                'I' or 'L' => '1',
                _ => c
            };
            if (!Alphabet.Contains(mapped) || code.Length == Length)
                return null;

            code.Append(mapped);
        }

        return code.Length == Length ? code.ToString() : null;
    }

    // Bilette ve ekranda okunaklı: "ABCD-EFGH-JKMN-PQRS".
    public static string Format(string code) => string.Join('-', code.Chunk(4).Select(part => new string(part)));
}
