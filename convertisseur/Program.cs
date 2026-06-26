using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace CalculatriceScientifique
{
    public class ParseException : Exception
    {
        public int Position { get; private set; }
        public string Expression { get; private set; }

        public ParseException(string message, int position, string expression) : base(message)
        {
            Position = position;
            Expression = expression;
        }
    }

    class Program
    {
        static List<string> historiqueCalculs = new List<string>();

        static void Main(string[] args)
        {
            CultureInfo cultureFr = new CultureInfo("fr-FR");
            bool continuer = true;

            while (continuer)
            {
                AfficherEnTete();

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(" Commandes : 'quitter', 'historique' ou 'equation'");
                Console.WriteLine(" Fonctions : sin, cos, tan, sqrt, log, exp, pi, e, i, ^, ²");
                Console.WriteLine(" Unités : mH, uF, kOhm, MHz, pF, nV, etc...");
                Console.Write("\n Entrez votre calcul : ");

                Console.ForegroundColor = ConsoleColor.Cyan;
                string saisie = Console.ReadLine();
                Console.ResetColor();

                if (string.IsNullOrWhiteSpace(saisie)) continue;

                string saisieMinuscule = saisie.ToLower().Trim();

                if (saisieMinuscule == "quitter")
                {
                    continuer = false;
                    continue;
                }

                if (saisieMinuscule == "historique")
                {
                    AfficherHistoriqueComplet();
                    continue;
                }

                if (saisieMinuscule == "equation" || saisieMinuscule == "équations" || saisieMinuscule == "equations")
                {
                    ModeEquation(cultureFr);
                    continue;
                }

                try
                {
                    List<string> notifications;
                    string saisiePretraitee = InterpreterUnites(saisie, out notifications);

                    foreach (string notif in notifications)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine(notif);
                    }

                    MathParser parser = new MathParser(saisiePretraitee);
                    Complex resultat = parser.Parse();

                    Console.Write("\n Analyse : ");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(parser.ExpressionNettoyee);

                    string resultatFormate = FormaterComplexe(resultat, cultureFr);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($" = {resultatFormate}");

                    historiqueCalculs.Add($"{saisie} = {resultatFormate}");
                }
                catch (ParseException ex)
                {
                    Console.Write("\n Analyse : ");
                    int safePos = Math.Max(0, Math.Min(ex.Position, ex.Expression.Length));

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(ex.Expression.Substring(0, safePos));

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex.Expression.Substring(safePos));

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($" Erreur de syntaxe : {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n Erreur : {ex.Message}");
                }

                Console.ResetColor();
                Console.WriteLine("\n Appuyez sur Entrée pour continuer...");
                Console.ReadLine();
            }
        }

        // ====================================================================
        // MOTEUR DE RÉSOLUTION D'ÉQUATIONS (INTELLIGENCE ALGEBRIQUE)
        // ====================================================================
        static void ModeEquation(CultureInfo culture)
        {
            bool modeActif = true;
            while (modeActif)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("======================================================");
                Console.WriteLine("             MODE ÉQUATION INTELLIGENT                ");
                Console.WriteLine("======================================================\n");
                Console.ResetColor();

                Console.WriteLine(" Tapez votre équation naturellement ! Le programme s'occupe de tout.\n");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(" [Exemples]   3*x + 5 = 14");
                Console.WriteLine("              x^2 - 5*x = -6");
                Console.WriteLine("              60 + x * e(-2.5) = 0");
                Console.WriteLine("\n (Astuce : Les espaces pour les systèmes marchent toujours)");
                Console.ResetColor();

                Console.Write("\n Équation (ou 'retour') : ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                string saisie = Console.ReadLine();
                Console.ResetColor();

                if (string.IsNullOrWhiteSpace(saisie)) continue;
                string saisieMin = saisie.ToLower().Trim();

                if (saisieMin == "retour" || saisieMin == "quitter")
                {
                    modeActif = false;
                    continue;
                }

                try
                {
                    // Si la phrase contient 'x' ou '=', on déclenche l'IA algébrique
                    if (saisieMin.Contains("x") || saisieMin.Contains("="))
                    {
                        string expression = saisieMin;
                        if (expression.Contains("="))
                        {
                            string[] parts = expression.Split('=');
                            // On déplace tout du même côté (Gauche - Droite = 0)
                            expression = $"({parts[0]}) - ({parts[1]})";
                        }

                        // Algorithme d'interpolation polynomiale pour deviner a, b, c
                        MathParser p0 = new MathParser(expression); p0.XValue = 0; Complex y0 = p0.Parse();
                        MathParser p1 = new MathParser(expression); p1.XValue = 1; Complex y1 = p1.Parse();
                        MathParser p_1 = new MathParser(expression); p_1.XValue = -1; Complex y_1 = p_1.Parse();
                        MathParser p2 = new MathParser(expression); p2.XValue = 2; Complex y2 = p2.Parse();

                        Complex a = (y1 + y_1 - 2.0 * y0) / 2.0;
                        Complex b = (y1 - y_1) / 2.0;
                        Complex c = y0;

                        // Vérification mathématique pour être sûr que c'est bien une équation de degré max 2
                        Complex check = a * 4.0 + b * 2.0 + c;

                        Console.WriteLine("\n --- DÉTAIL DU CALCUL ---");

                        if (Complex.Abs(check - y2) > 1e-5)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(" Erreur : L'équation est trop complexe (ex: x à la puissance 3, ou sin(x)).");
                            Console.WriteLine(" Je ne peux résoudre que des polynômes du 1er ou 2nd degré.");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            if (Complex.Abs(a) < 1e-10)
                            {
                                // 1er Degré
                                double coefA = b.Real, coefB = c.Real;
                                Console.WriteLine($" Équation déduite : {FormaterNombre(coefA, culture)}x + ({FormaterNombre(coefB, culture)}) = 0");

                                if (Math.Abs(coefA) < 1e-10) Console.WriteLine(Math.Abs(coefB) < 1e-10 ? " Infinité de solutions." : " Impossible.");
                                else
                                {
                                    Console.WriteLine($" -> {FormaterNombre(coefA, culture)}x = {FormaterNombre(-coefB, culture)}");
                                    double xRes = -coefB / coefA;
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($"\n Résultat : x = {FormaterNombre(xRes, culture)}");
                                }
                            }
                            else
                            {
                                // 2nd Degré
                                double coefA = a.Real, coefB = b.Real, coefC = c.Real;
                                Console.WriteLine($" Équation déduite : {FormaterNombre(coefA, culture)}x² + ({FormaterNombre(coefB, culture)})x + ({FormaterNombre(coefC, culture)}) = 0");

                                double delta = (coefB * coefB) - (4 * coefA * coefC);
                                Console.WriteLine($" -> Calcul de Delta (Δ) = b² - 4ac");
                                Console.WriteLine($" -> Δ = ({FormaterNombre(coefB, culture)})² - 4*({FormaterNombre(coefA, culture)})*({FormaterNombre(coefC, culture)}) = {FormaterNombre(delta, culture)}");

                                Console.ForegroundColor = ConsoleColor.Green;
                                if (delta > 0)
                                {
                                    Console.WriteLine("\n Δ > 0 : Deux racines réelles.");
                                    double rDelta = Math.Sqrt(delta);
                                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                                    Console.WriteLine($" -> √Δ = {FormaterNombre(rDelta, culture)}");
                                    double x1 = (-coefB - rDelta) / (2 * coefA);
                                    double x2 = (-coefB + rDelta) / (2 * coefA);
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.WriteLine($"\n x1 = {FormaterNombre(x1, culture)}");
                                    Console.WriteLine($" x2 = {FormaterNombre(x2, culture)}");
                                }
                                else if (Math.Abs(delta) < 1e-10)
                                {
                                    Console.WriteLine("\n Δ = 0 : Une racine double.");
                                    double x0 = -coefB / (2 * coefA);
                                    Console.WriteLine($"\n x0 = {FormaterNombre(x0, culture)}");
                                }
                                else
                                {
                                    Console.WriteLine("\n Δ < 0 : Deux racines complexes.");
                                    double rDelta = Math.Sqrt(-delta);
                                    double reel = -coefB / (2 * coefA);
                                    double imag = Math.Abs(rDelta / (2 * coefA));

                                    string strReel = FormaterNombre(reel, culture);
                                    string strImag = FormaterNombre(imag, culture);
                                    if (Math.Abs(reel) < 1e-10) strReel = "0";

                                    Console.WriteLine($"\n x1 = {strReel} - {strImag}i");
                                    Console.WriteLine($" x2 = {strReel} + {strImag}i");
                                }
                            }
                        }
                    }
                    else
                    {
                        // ANCIEN MODE DE SAISIE PAR ESPACES (Pour les systèmes par ex.)
                        string[] strParts = saisie.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        double[] val = new double[strParts.Length];

                        for (int i = 0; i < strParts.Length; i++)
                        {
                            MathParser parser = new MathParser(strParts[i]);
                            Complex comp = parser.Parse();
                            val[i] = comp.Real;
                        }

                        Console.WriteLine("\n --- DÉTAIL DU CALCUL ---");
                        Console.ForegroundColor = ConsoleColor.DarkYellow;

                        if (val.Length == 6)
                        {
                            double a = val[0], b = val[1], c = val[2];
                            double d = val[3], e = val[4], f = val[5];

                            Console.WriteLine($" Système reconnu :");
                            Console.WriteLine($"  (1)  {FormaterNombre(a, culture)}x + ({FormaterNombre(b, culture)})y = {FormaterNombre(c, culture)}");
                            Console.WriteLine($"  (2)  {FormaterNombre(d, culture)}x + ({FormaterNombre(e, culture)})y = {FormaterNombre(f, culture)}\n");

                            Console.WriteLine($" -> Méthode de Cramer (Déterminants)");
                            double detPrincipal = (a * e) - (b * d);
                            Console.WriteLine($" -> D  = (a*e - b*d) = {FormaterNombre(detPrincipal, culture)}");

                            if (Math.Abs(detPrincipal) < 1e-10)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("\n Le déterminant est nul (Droites parallèles ou confondues, pas de solution unique).");
                            }
                            else
                            {
                                double dx = (c * e) - (b * f);
                                double dy = (a * f) - (c * d);
                                Console.WriteLine($" -> Dx = (c*e - b*f) = {FormaterNombre(dx, culture)}");
                                Console.WriteLine($" -> Dy = (a*f - c*d) = {FormaterNombre(dy, culture)}");

                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"\n x = Dx / D = {FormaterNombre(dx / detPrincipal, culture)}");
                                Console.WriteLine($" y = Dy / D = {FormaterNombre(dy / detPrincipal, culture)}");
                            }
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(" Nombre de paramètres invalide ! Si vous ne mettez pas de 'x', il faut 6 coefficients pour un système.");
                        }
                    }
                }
                catch (Exception)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n Erreur de syntaxe de l'équation.");
                }

                Console.ResetColor();
                Console.WriteLine("\n Appuyez sur Entrée pour un autre calcul...");
                Console.ReadLine();
            }
        }
        // ====================================================================

        static string InterpreterUnites(string saisie, out List<string> notifications)
        {
            List<string> notifsLocales = new List<string>();
            string pattern = @"([0-9]+(?:[\.,][0-9]+)?)\s*([a-zA-Zµ]+)";

            string saisieModifiee = Regex.Replace(saisie, pattern, match =>
            {
                string nombreStr = match.Groups[1].Value;
                string suffixe = match.Groups[2].Value;

                char c1 = suffixe[0];
                string reste = suffixe.Length > 1 ? suffixe.Substring(1) : "";

                if (EstUniteConnue(suffixe))
                {
                    notifsLocales.Add($"   [Info] Unité pure ignorée : {nombreStr} {suffixe}");
                    return nombreStr;
                }

                string puissance = "";
                string nomPrefixe = "";

                switch (c1)
                {
                    case 'T': puissance = "*10^12"; nomPrefixe = "Téra"; break;
                    case 'G': puissance = "*10^9"; nomPrefixe = "Giga"; break;
                    case 'M': puissance = "*10^6"; nomPrefixe = "Méga"; break;
                    case 'k':
                    case 'K': puissance = "*10^3"; nomPrefixe = "kilo"; break;
                    case 'm': puissance = "*10^-3"; nomPrefixe = "milli"; break;
                    case 'u':
                    case 'µ': puissance = "*10^-6"; nomPrefixe = "micro"; break;
                    case 'n': puissance = "*10^-9"; nomPrefixe = "nano"; break;
                    case 'p': puissance = "*10^-12"; nomPrefixe = "pico"; break;
                }

                if (puissance != "" && (reste == "" || EstUniteConnue(reste)))
                {
                    string nomUnite = reste != "" ? $" {reste}" : "";
                    string affichagePuissance = puissance.Replace("*", "x ");
                    notifsLocales.Add($"   [Info] Conversion auto : {nombreStr}{suffixe} -> {nombreStr} {nomPrefixe}{nomUnite.Trim()} ({affichagePuissance})");

                    return $"({nombreStr}{puissance})";
                }

                return match.Value;
            });

            notifications = notifsLocales;
            return saisieModifiee;
        }

        static bool EstUniteConnue(string str)
        {
            string s = str.ToLower();
            return s == "h" || s == "f" || s == "v" || s == "a" || s == "ohm" || s == "ohms" || s == "w" || s == "hz" || s == "s" || s == "c";
        }

        static void AfficherEnTete()
        {
            try { if (!Console.IsOutputRedirected) Console.Clear(); }
            catch (System.IO.IOException) { Console.WriteLine("\n--- NOUVEAU CALCUL ---"); }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(@"
  ======================================================
 |    ____      _            _       ____  ____   ___   |
 |   / ___|__ _| | ___      / \     |  _ \|  _ \ / _ \  |
 |  | |   / _` | |/ __|____/ _ \    | |_) | |_) | | | | |
 |  | |__| (_| | | (_|_____/ ___ \  |  __/|  _ <| |_| | |
 |   \____\__,_|_|\___|   /_/   \_\ |_|   |_| \_\\___/  |
 |                                                      |
 |    CALCULATRICE SCIENTIFIQUE & NOMBRES COMPLEXES     |
  ======================================================
");

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(" --- APERÇU DE L'HISTORIQUE (RAM) ---");
            if (historiqueCalculs.Count == 0)
            {
                Console.WriteLine(" (Aucun calcul pour le moment)");
            }
            else
            {
                int depart = Math.Max(0, historiqueCalculs.Count - 3);
                for (int i = depart; i < historiqueCalculs.Count; i++)
                {
                    Console.WriteLine($" [{i + 1}] {historiqueCalculs[i]}");
                }
            }
            Console.WriteLine(" ------------------------------------\n");
            Console.ResetColor();
        }

        static void AfficherHistoriqueComplet()
        {
            try { if (!Console.IsOutputRedirected) Console.Clear(); } catch { }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("======================================================");
            Console.WriteLine("          HISTORIQUE COMPLET DE LA SESSION            ");
            Console.WriteLine("======================================================\n");
            Console.ResetColor();

            if (historiqueCalculs.Count == 0)
            {
                Console.WriteLine(" L'historique est vide.");
            }
            else
            {
                for (int i = 0; i < historiqueCalculs.Count; i++)
                {
                    Console.WriteLine($" {i + 1}. {historiqueCalculs[i]}");
                }
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n======================================================");
            Console.ResetColor();
            Console.WriteLine(" Appuyez sur Entrée pour revenir à la calculatrice...");
            Console.ReadLine();
        }

        static string FormaterNombre(double nombre, CultureInfo culture)
        {
            if (Math.Abs(nombre) < 1e-10) return "0";
            string brut = nombre.ToString("G3", culture);
            return brut.Replace("E+", " x 10^").Replace("E-", " x 10^-").Replace("E", " x 10^");
        }

        static string FormaterComplexe(Complex c, CultureInfo culture)
        {
            string reel = FormaterNombre(c.Real, culture);
            string imag = FormaterNombre(c.Imaginary, culture);

            if (Math.Abs(c.Imaginary) < 1e-10) return reel;

            if (Math.Abs(c.Real) < 1e-10)
            {
                if (imag == "1") return "i";
                if (imag == "-1") return "-i";
                return imag + "i";
            }

            string operateur = c.Imaginary > 0 ? "+" : "-";
            string absImag = FormaterNombre(Math.Abs(c.Imaginary), culture);
            string imagStr = absImag == "1" ? "i" : absImag + "i";

            return $"{reel} {operateur} {imagStr}";
        }
    }

    public class MathParser
    {
        private int pos = -1;
        private int ch;
        private string str;

        // NOUVEAU : Propriété pour remplacer le x par une valeur de test
        public Complex XValue { get; set; } = 0;

        public string ExpressionNettoyee { get { return str; } }

        public MathParser(string expression)
        {
            str = expression.ToLower().Replace(" ", "").Replace(',', '.').Replace("²", "^2").Replace("³", "^3");

            // Multiplication implicite classique (ex: 47i -> 47*i, 2pi -> 2*pi)
            str = Regex.Replace(str, @"([0-9])([a-z])", "$1*$2");

            // Multiplication implicite pour le x (ex: xsin -> x*sin, xe -> x*e)
            str = str.Replace("xe", "x*e").Replace("xsin", "x*sin").Replace("xcos", "x*cos").Replace("xtan", "x*tan")
                     .Replace("xlog", "x*log").Replace("xsqr", "x*sqr").Replace("xpi", "x*pi");
        }

        private void NextChar()
        {
            ch = (++pos < str.Length) ? str[pos] : -1;
        }

        private bool Eat(int charToEat)
        {
            while (ch == ' ') NextChar();
            if (ch == charToEat)
            {
                NextChar();
                return true;
            }
            return false;
        }

        public Complex Parse()
        {
            NextChar();
            Complex x = ParseExpression();
            if (pos < str.Length) throw new ParseException("Caractère inattendu : '" + (char)ch + "'", pos, str);
            return x;
        }

        private Complex ParseExpression()
        {
            Complex x = ParseTerm();
            for (; ; )
            {
                if (Eat('+')) x += ParseTerm();
                else if (Eat('-')) x -= ParseTerm();
                else return x;
            }
        }

        private Complex ParseTerm()
        {
            Complex x = ParseFactor();
            for (; ; )
            {
                if (Eat('*')) x *= ParseFactor();
                else if (Eat('/')) x /= ParseFactor();
                else return x;
            }
        }

        private Complex ParseFactor()
        {
            Complex x = ParsePrimary();
            if (Eat('^')) x = Complex.Pow(x, ParseFactor());
            return x;
        }

        private Complex ParsePrimary()
        {
            if (Eat('('))
            {
                Complex x = ParseExpression();
                Eat(')');
                return x;
            }

            if (Eat('+')) return ParsePrimary();
            if (Eat('-')) return -ParsePrimary();

            int startPos = this.pos;
            if ((ch >= '0' && ch <= '9') || ch == '.')
            {
                while ((ch >= '0' && ch <= '9') || ch == '.') NextChar();
                return double.Parse(str.Substring(startPos, this.pos - startPos), CultureInfo.InvariantCulture);
            }

            if (ch >= 'a' && ch <= 'z')
            {
                while (ch >= 'a' && ch <= 'z') NextChar();
                string func = str.Substring(startPos, this.pos - startPos);

                if (func == "i") return Complex.ImaginaryOne;
                if (func == "pi") return Math.PI;

                // Le fameux 'x' est lu ici
                if (func == "x") return XValue;

                // Si tu tapes e(-2.5) au lieu de exp(-2.5), le programme le devine ici
                if (func == "e")
                {
                    int tempPos = pos;
                    while (tempPos < str.Length && str[tempPos] == ' ') tempPos++;
                    if (tempPos < str.Length && str[tempPos] == '(')
                    {
                        func = "exp";
                    }
                    else
                    {
                        return Math.E;
                    }
                }

                Complex arg = ParsePrimary();
                if (func == "sqrt") return Complex.Sqrt(arg);
                if (func == "sin") return Complex.Sin(arg);
                if (func == "cos") return Complex.Cos(arg);
                if (func == "tan") return Complex.Tan(arg);
                if (func == "log") return Complex.Log(arg);
                if (func == "exp") return Complex.Exp(arg);

                throw new ParseException("Fonction inconnue : '" + func + "'", startPos, str);
            }

            throw new ParseException("Caractère inattendu : '" + (char)ch + "'", pos, str);
        }
    }
}