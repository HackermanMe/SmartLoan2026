using System;

namespace EcoService.Services.Export
{
    /// <summary>
    /// Convertit un nombre entier en texte français (ex: 208177 → "Deux cent huit mille cent soixante-dix-sept")
    /// </summary>
    public static class NombreEnLettres
    {
        private static readonly string[] Unites =
        {
            "", "un", "deux", "trois", "quatre", "cinq", "six", "sept",
            "huit", "neuf", "dix", "onze", "douze", "treize", "quatorze",
            "quinze", "seize", "dix-sept", "dix-huit", "dix-neuf"
        };

        private static readonly string[] Dizaines =
        {
            "", "", "vingt", "trente", "quarante", "cinquante",
            "soixante", "soixante", "quatre-vingt", "quatre-vingt"
        };

        public static string Convertir(long nombre)
        {
            if (nombre == 0) return "zéro";
            if (nombre < 0) return "moins " + Convertir(-nombre);

            string result = "";

            // Milliards
            if (nombre >= 1_000_000_000)
            {
                long n = nombre / 1_000_000_000;
                result += ConvertirMoinsDeMillier(n) + " milliard" + (n > 1 ? "s" : "");
                nombre %= 1_000_000_000;
                if (nombre > 0) result += " ";
            }

            // Millions
            if (nombre >= 1_000_000)
            {
                long n = nombre / 1_000_000;
                result += ConvertirMoinsDeMillier(n) + " million" + (n > 1 ? "s" : "");
                nombre %= 1_000_000;
                if (nombre > 0) result += " ";
            }

            // Milliers
            if (nombre >= 1000)
            {
                long n = nombre / 1000;
                result += (n == 1) ? "mille" : ConvertirMoinsDeMillier(n) + " mille";
                nombre %= 1000;
                if (nombre > 0) result += " ";
            }

            if (nombre > 0)
                result += ConvertirMoinsDeMillier(nombre);

            return result;
        }

        /// <summary>
        /// Convertit et met la première lettre en majuscule.
        /// Ex: 208177 → "Deux cent huit mille cent soixante-dix-sept"
        /// </summary>
        public static string ConvertirMajuscule(long nombre)
        {
            string s = Convertir(nombre);
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpper(s[0]) + s.Substring(1);
        }

        /// <summary>
        /// Format monétaire complet.
        /// Ex: 11000000 → "Onze millions francs CFA"
        /// </summary>
        public static string ConvertirFrancsCFA(long nombre)
        {
            return ConvertirMajuscule(nombre) + " francs CFA";
        }

        private static string ConvertirMoinsDeMillier(long nombre)
        {
            string result = "";

            if (nombre >= 100)
            {
                long centaines = nombre / 100;
                result += (centaines == 1) ? "cent" : Unites[centaines] + " cent";
                nombre %= 100;

                if (nombre == 0 && centaines > 1)
                    result += "s";      // "deux cents" (avec s)
                else if (nombre > 0)
                    result += " ";      // "deux cent " (sans s, suivi)
            }

            if (nombre >= 20)
            {
                int dizaine = (int)(nombre / 10);
                int unite = (int)(nombre % 10);

                if (dizaine == 7 || dizaine == 9)
                {
                    // 70-79 : soixante-dix...  /  90-99 : quatre-vingt-dix...
                    int sub = 10 + unite;
                    result += Dizaines[dizaine];
                    if (sub == 11 && dizaine == 7)
                        result += " et onze";       // 71 = soixante et onze
                    else
                        result += "-" + Unites[sub]; // 70=soixante-dix, 72=soixante-douze...
                }
                else if (dizaine == 8)
                {
                    result += "quatre-vingt";
                    if (unite == 0)
                        result += "s";               // 80 = quatre-vingts
                    else
                        result += "-" + Unites[unite]; // 81 = quatre-vingt-un
                }
                else
                {
                    // 20-69
                    result += Dizaines[dizaine];
                    if (unite == 1)
                        result += " et un";          // 21=vingt et un, 31=trente et un...
                    else if (unite > 0)
                        result += "-" + Unites[unite]; // 22=vingt-deux...
                }
            }
            else if (nombre > 0)
            {
                result += Unites[nombre];
            }

            return result;
        }
    }
}
