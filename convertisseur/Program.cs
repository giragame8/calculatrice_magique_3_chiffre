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
        // MOTEUR DE RÉSOLUTION D'ÉQUATIONS
        // ====================================================================
        static void ModeEquation(CultureInfo culture)
        {
            bool modeActif = true;
            while (modeActif)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("======================================================");
                Console.WriteLine("                MODE ÉQUATION RAPIDE                  ");
                Console.WriteLine("======================================================\n");
                Console.ResetColor();

                Console.WriteLine(" Pas de menu ! Tapez juste les nombres séparés par un espace :\n");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(" [1er Degré]    ax + b = 0            -> Tapez : a b");
                Console.WriteLine(" [2nd Degré]    ax² + bx + c = 0      -> Tapez : a b c");
                Console.WriteLine(" [Système]      ax+by=c et dx+ey=f    -> Tapez : a b c d e f");
                Console.WriteLine("\n Exemple 2nd degré : 1 -5 6 (pour x² - 5x + 6 = 0)");
                Console.ResetColor();

                Console.Write("\n Coefficients (ou 'retour') : ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                string saisie = Console.ReadLine();
                Console.ResetColor();

                if (string.IsNullOrWhiteSpace(saisie)) continue;
                if (saisie.ToLower().Trim() == "retour" || saisie.ToLower().Trim() == "quitter")
                {
                    modeActif = false;
                    continue;
                }

                try
                {
                    string[] strParts = saisie.Replace('.', ',').Split(new char[] { ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    double[] val = new double[strParts.Length];

                    for (int i = 0; i < strParts.Length; i++)
                    {
                        val[i] = double.Parse(strParts[i], culture);
                    }

                    Console.WriteLine("\n --- DÉTAIL DU CALCUL ---");
                    Console.ForegroundColor = ConsoleColor.DarkYellow;

                    if (val.Length == 2)
                    {
                        double a = val[0], b = val[1];
                        Console.WriteLine($" Équation : {a}x + ({b}) = 0");

                        if (a == 0) Console.WriteLine(b == 0 ? " Infinité de solutions." : " Impossible.");
                        else
                        {
                            Console.WriteLine($" -> {a}x = {-b}");
                            double x = -b / a;
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"\n Résultat : x = {FormaterNombre(x, culture)}");
                        }
                    }
                    else if (val.Length == 3)
                    {
                        double a = val[0], b = val[1], c = val[2];
                        Console.WriteLine($" Équation : {a}x² + ({b})x + ({c}) = 0");

                        if (a == 0) Console.WriteLine(" Erreur : 'a' est nul, ce n'est pas du 2nd degré.");
                        else
                        {
                            double delta = (b * b) - (4 * a * c);
                            Console.WriteLine($" -> Calcul de Delta (Δ) = b² - 4ac");
                            Console.WriteLine($" -> Δ = ({b})² - 4*({a})*({c}) = {FormaterNombre(delta, culture)}");

                            Console.ForegroundColor = ConsoleColor.Green;
                            if (delta > 0)
                            {
                                Console.WriteLine("\n Δ > 0 : Deux racines réelles.");
                                double rDelta = Math.Sqrt(delta);
                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                                Console.WriteLine($" -> √Δ = {FormaterNombre(rDelta, culture)}");
                                double x1 = (-b - rDelta) / (2 * a);
                                double x2 = (-b + rDelta) / (2 * a);
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"\n x1 = {FormaterNombre(x1, culture)}");
                                Console.WriteLine($" x2 = {FormaterNombre(x2, culture)}");
                            }
                            else if (Math.Abs(delta) < 1e-10)
                            {
                                Console.WriteLine("\n Δ = 0 : Une racine double.");
                                double x0 = -b / (2 * a);
                                Console.WriteLine($"\n x0 = {FormaterNombre(x0, culture)}");
                            }
                            else
                            {
                                Console.WriteLine("\n Δ < 0 : Deux racines complexes.");
                                double rDelta = Math.Sqrt(-delta);
                                double reel = -b / (2 * a);
                                double imag = Math.Abs(rDelta / (2 * a));

                                string strReel = FormaterNombre(reel, culture);
                                string strImag = FormaterNombre(imag, culture);
                                if (Math.Abs(reel) < 1e-10) strReel = "0";

                                Console.WriteLine($"\n x1 = {strReel} - {strImag}i");
                                Console.WriteLine($" x2 = {strReel} + {strImag}i");
                            }
                        }
                    }
                    else if (val.Length == 6)
                    {
                        double a = val[0], b = val[1], c = val[2];
                        double d = val[3], e = val[4], f = val[5];

                        Console.WriteLine($" Système :");
                        Console.WriteLine($"  (1)  {a}x + ({b})y = {c}");
                        Console.WriteLine($"  (2)  {d}x + ({e})y = {f}\n");

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
                        Console.WriteLine(" Nombre de coefficients invalide ! Tapez 2, 3 ou 6 nombres.");
                    }
                }
                catch (Exception)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n Erreur de syntaxe. Assurez-vous de n'entrer que des nombres.");
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

        public string ExpressionNettoyee { get { return str; } }

        public MathParser(string expression)
        {
            // On nettoie les espaces, gère les virgules, et remplace les exposants
            str = expression.ToLower().Replace(" ", "").Replace(',', '.').Replace("²", "^2").Replace("³", "^3");

            // CORRECTION IMPORTANTE ICI : Multiplication implicite
            // Remplace un Chiffre suivi d'une Lettre par Chiffre*Lettre (ex: 47i -> 47*i, 2pi -> 2*pi)
            str = Regex.Replace(str, @"([0-9])([a-z])", "$1*$2");
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
                if (func == "e") return Math.E;

                Complex x = ParsePrimary();
                if (func == "sqrt") return Complex.Sqrt(x);
                if (func == "sin") return Complex.Sin(x);
                if (func == "cos") return Complex.Cos(x);
                if (func == "tan") return Complex.Tan(x);
                if (func == "log") return Complex.Log(x);
                if (func == "exp") return Complex.Exp(x);

                throw new ParseException("Fonction inconnue : '" + func + "'", startPos, str);
            }

            throw new ParseException("Caractère inattendu : '" + (char)ch + "'", pos, str);
        }
    }
}