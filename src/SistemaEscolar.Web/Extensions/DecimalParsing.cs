using System.Globalization;

namespace SistemaEscolar.Web.Extensions;

public static class DecimalParsing
{
    // Interpreta o texto de um campo de nota (0 a 10) vindo do formulário/AJAX. Aceita tanto ponto
    // quanto vírgula como separador decimal — o padrão brasileiro é vírgula (ex.: "7,5").
    //
    // Antes, em vários lugares, o código tentava primeiro decimal.TryParse com NumberStyles.Number e
    // CultureInfo.InvariantCulture, cujo separador de milhar TAMBÉM é a vírgula: "7,5" não falhava, virava
    // 75 (a vírgula era lida como separador de milhar, não decimal). Isso rejeitava silenciosamente notas
    // válidas como "7,5" ou "8,5" com o erro genérico de "nota inválida". Normalizar a vírgula para ponto
    // antes de interpretar evita essa ambiguidade.
    public static bool TryParseNota(string? rawValue, out decimal? value)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            value = null;
            return true;
        }

        var texto = rawValue.Trim().Replace(",", ".");
        if (decimal.TryParse(texto, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsed))
        {
            value = parsed;
            return true;
        }

        value = null;
        return false;
    }
}
