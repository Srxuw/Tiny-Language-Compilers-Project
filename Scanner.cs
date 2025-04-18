using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;


namespace JASON_Compiler
{
    public enum Token_Class
    {
        // Keywords
        Begin, Call, Declare, End, Do, Else, EndIf, EndUntil, EndWhile, If, Repeat,
        Integer, Real, String, Parameters, Procedure, Program, Read, Set, Then, Until,
        While, Write, Return, Function, Var, Const, Main, Break, Continue, Datatype_int, Datatype_float, Datatype_string,

        // Symbols
        Dot, Semicolon, Comma, LParanthesis, RParanthesis, LCurlyParanthesis, RCurlyParanthesis,

        // Operators
        ConditionalOp, ConditionalOpEquals, ConditionalOpLessThan, ConditionalOpGreaterThan, BoolAnd, BoolOr, ArithOp, ArithOpPlus, ArithOpMinus, ArithOpDivide, ArithOpMultiply, Increment, Decrement,

        // Others
        Identifier, Constant, Comment, Undefined
    }

    public class Token
    {
        public string lex;
        public Token_Class token_type;
        public string description;
    }

    public class Scanner
    {
        static HashSet<string> FunctionsWithDatatypes = new HashSet<string>();
        public List<Token> Tokens = new List<Token>();
        Dictionary<string, Token_Class> ReservedWords = new Dictionary<string, Token_Class>();
        Dictionary<string, Token_Class> Operators = new Dictionary<string, Token_Class>();
        Dictionary<string, Token_Class> Delimiters = new Dictionary<string, Token_Class>()




            {
                { ";", Token_Class.Semicolon },
                { ",", Token_Class.Comma },
                { "(", Token_Class.LParanthesis },
                { ")", Token_Class.RParanthesis },
                { "{", Token_Class.LCurlyParanthesis },
                { "}", Token_Class.RCurlyParanthesis },
            };

        List<string> NonSemicolonEndKeywords = new List<string>()
            {
                "IF", "WHILE", "UNTIL", "DO", "FUNCTION", "MAIN", "BEGIN", "END",
                "ELSE", "ENDIF", "ENDWHILE", "ENDUNTIL", "RETURN", "BREAK", "CONTINUE"
            };

        public Scanner()
        {
            ReservedWords.Add("IF", Token_Class.If);
            ReservedWords.Add("BEGIN", Token_Class.Begin);
            ReservedWords.Add("CALL", Token_Class.Call);
            ReservedWords.Add("DECLARE", Token_Class.Declare);
            ReservedWords.Add("END", Token_Class.End);
            ReservedWords.Add("DO", Token_Class.Do);
            ReservedWords.Add("ELSE", Token_Class.Else);
            ReservedWords.Add("ENDIF", Token_Class.EndIf);
            ReservedWords.Add("ENDUNTIL", Token_Class.EndUntil);
            ReservedWords.Add("ENDWHILE", Token_Class.EndWhile);
            ReservedWords.Add("INTEGER", Token_Class.Integer);
            ReservedWords.Add("REAL", Token_Class.Real);
            ReservedWords.Add("STRING", Token_Class.String);
            ReservedWords.Add("INT", Token_Class.Integer);
            ReservedWords.Add("FLOAT", Token_Class.Real);
            ReservedWords.Add("REPEAT", Token_Class.Repeat);
            ReservedWords.Add("PARAMETERS", Token_Class.Parameters);
            ReservedWords.Add("PROCEDURE", Token_Class.Procedure);
            ReservedWords.Add("PROGRAM", Token_Class.Program);
            ReservedWords.Add("READ", Token_Class.Read);
            ReservedWords.Add("SET", Token_Class.Set);
            ReservedWords.Add("THEN", Token_Class.Then);
            ReservedWords.Add("UNTIL", Token_Class.Until);
            ReservedWords.Add("WHILE", Token_Class.While);
            ReservedWords.Add("WRITE", Token_Class.Write);
            ReservedWords.Add("RETURN", Token_Class.Return);
            ReservedWords.Add("FUNCTION", Token_Class.Function);
            ReservedWords.Add("VAR", Token_Class.Var);
            ReservedWords.Add("CONST", Token_Class.Const);
            ReservedWords.Add("MAIN", Token_Class.Main);

            ReservedWords.Add("BREAK", Token_Class.Break);
            ReservedWords.Add("CONTINUE", Token_Class.Continue);

            Operators.Add("=", Token_Class.ConditionalOp);
            Operators.Add(":=", Token_Class.ConditionalOpEquals);
            Operators.Add("<", Token_Class.ConditionalOpLessThan);
            Operators.Add(">", Token_Class.ConditionalOpGreaterThan);
            Operators.Add("<>", Token_Class.ConditionalOp);
            Operators.Add("&&", Token_Class.BoolAnd);
            Operators.Add("||", Token_Class.BoolOr);
            Operators.Add("+", Token_Class.ArithOpPlus);
            Operators.Add("-", Token_Class.ArithOpMinus);
            Operators.Add("–", Token_Class.ArithOpMinus);
            Operators.Add("*", Token_Class.ArithOpMultiply);
            Operators.Add("/", Token_Class.ArithOpDivide);
            Operators.Add("++", Token_Class.Increment);
            Operators.Add("--", Token_Class.Decrement);
        }

        public void StartScanning(string SourceCode)
        {
            Tokens.Clear();
            JASON_Compiler.TokenStream.Clear();
            Errors.Clear();

            string[] lines = SourceCode.Split('\n');
            HashSet<string> DeclaredFunctions = new HashSet<string>();
            int openCurlyCount = 0;
            int openBracketCount = 0;
            bool lineIsControlKeyword = false;

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string rawLine = lines[lineIndex];
                string line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                bool isComment = false;
                bool isFunctionHeader = false;
                bool hasBrace = line.Contains("{") || line.Contains("}");
                bool hasBracket = line.Contains("(") || line.Contains(")");
                bool endsWithSemicolon = line.TrimEnd().EndsWith(";");

                int i = 0;
                while (i < line.Length)
                {
                    if (char.IsWhiteSpace(line[i])) { i++; continue; }

                    string lexeme = "";
                    char currentChar = line[i];

                    // ----------- Handle Comment -----------
                    if (currentChar == '/' && i + 1 < line.Length && line[i + 1] == '*')
                    {
                        lexeme += "/*";
                        i += 2;
                        bool closed = false;
                        while (i + 1 < line.Length)
                        {
                            if (line[i] == '*' && line[i + 1] == '/')
                            {
                                lexeme += "*/";
                                i += 2;
                                closed = true;
                                break;
                            }
                            lexeme += line[i++];
                        }
                        if (!closed) Errors.Add($"Unclosed comment: '{lexeme}'");
                        isComment = true;
                        break;
                    }

                    // ----------- Handle String -----------
                    if (currentChar == '"')
                    {
                        i++;
                        lexeme = "\"";
                        bool closed = false;
                        while (i < line.Length)
                        {
                            if (line[i] == '"')
                            {
                                lexeme += '"';
                                i++;
                                closed = true;
                                break;
                            }
                            lexeme += line[i++];
                        }
                        if (closed)
                            Tokens.Add(new Token { lex = lexeme, token_type = Token_Class.String, description = "String constant" });
                        else
                            Errors.Add($"Unclosed string: '{lexeme}'");
                        continue;
                    }

                    // ----------- Handle Operators -----------
                    bool matchedOp = false;
                    foreach (var op in new List<string> { ":=", "++", "--", "<>", "&&", "||", "+", "-", "*", "/", "=", "<", ">" })
                    {
                        if (line.Substring(i).StartsWith(op))
                        {
                            lexeme = op;
                            Tokens.Add(new Token { lex = lexeme, token_type = Operators[op], description = $"{op} operator" });
                            i += op.Length;
                            matchedOp = true;

                            // Check for extra ++/--/<<<
                            while (i < line.Length && line[i] == op[0])
                            {
                                Errors.Add($"Invalid character: '{line[i]}'");
                                i++;
                            }

                            break;
                        }
                    }
                    if (matchedOp) continue;

                    // ----------- Handle Delimiters -----------
                    if (Delimiters.ContainsKey(currentChar.ToString()))
                    {
                        lexeme = currentChar.ToString();
                        Tokens.Add(new Token { lex = lexeme, token_type = Delimiters[lexeme], description = "Delimiter" });

                        if (currentChar == '{') openCurlyCount++;
                        if (currentChar == '}') openCurlyCount--;
                        if (currentChar == '(') openBracketCount++;
                        if (currentChar == ')') openBracketCount--;
                        i++;
                        continue;
                    }

                    // ----------- Handle Numbers -----------
                    if (char.IsDigit(currentChar))
                    {
                        bool hasDot = false, valid = true;
                        while (i < line.Length && (char.IsDigit(line[i]) || line[i] == '.'))
                        {
                            if (line[i] == '.')
                            {
                                if (hasDot) valid = false;
                                hasDot = true;
                            }
                            lexeme += line[i++];
                        }

                        if (i < line.Length && char.IsLetter(line[i]))
                        {
                            valid = false;
                            while (i < line.Length && !char.IsWhiteSpace(line[i]) &&
                                !Delimiters.ContainsKey(line[i].ToString()) &&
                                !Operators.ContainsKey(line[i].ToString()))

                                lexeme += line[i++];
                        }

                        if (valid && Regex.IsMatch(lexeme, @"^\d+(\.\d+)?$"))
                            Tokens.Add(new Token { lex = lexeme, token_type = Token_Class.Constant, description = "Numeric constant" });
                        else
                            Errors.Add($"Invalid constant: '{lexeme}'");
                        continue;
                    }


                    // ----------- Handle Identifiers / Keywords / Functions -----------
                    if (char.IsLetter(currentChar))
                    {
                        while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '_'))
                            lexeme += line[i++];

                        string upperLex = lexeme.ToUpper();
                        string lowerLex = lexeme.ToLower();
                        string remaining = line.Substring(i).TrimStart();
                        bool isFollowedByParen = remaining.StartsWith("(");

                        bool isValidIdentifier = Regex.IsMatch(lexeme, @"^[A-Za-z_][A-Za-z0-9_]*$");

                        // --- Handle lowercase data types ---
                        if (lowerLex == "int" && lexeme == "int")
                        {
                            Tokens.Add(new Token { lex = lexeme, token_type = Token_Class.Datatype_int, description = "Datatype int" });
                            string afterType = line.Substring(i).TrimStart();
                            if (!Regex.IsMatch(afterType, @"^[A-Za-z_][A-Za-z0-9_]*"))
                                Errors.Add($"Missing identifier after datatype '{lexeme}'");
                            continue;
                        }
                        else if (lowerLex == "float" && lexeme == "float")
                        {
                            Tokens.Add(new Token { lex = lexeme, token_type = Token_Class.Datatype_float, description = "Datatype float" });
                            string afterType = line.Substring(i).TrimStart();
                            if (!Regex.IsMatch(afterType, @"^[A-Za-z_][A-Za-z0-9_]*"))
                                Errors.Add($"Missing identifier after datatype '{lexeme}'");
                            continue;
                        }
                        else if (lowerLex == "string" && lexeme == "string")
                        {
                            Tokens.Add(new Token { lex = lexeme, token_type = Token_Class.Datatype_string, description = "Datatype string" });
                            string afterType = line.Substring(i).TrimStart();
                            if (!Regex.IsMatch(afterType, @"^[A-Za-z_][A-Za-z0-9_]*"))
                                Errors.Add($"Missing identifier after datatype '{lexeme}'");
                            continue;
                        }

                        // --- Handle reserved lowercase keywords ---
                        if (ReservedWords.ContainsKey(upperLex))
                        {
                            if (lexeme == lowerLex)
                            {
                                string desc = "Keyword";

                                // Handle datatypes again (uppercase version)
                                if (lowerLex == "int")
                                    desc = "Datatype int";
                                else if (lowerLex == "float")
                                    desc = "Datatype float";
                                else if (lowerLex == "string")
                                    desc = "Datatype string";
                                else
                                    desc = lowerLex + "_Statement";
                                if (lowerLex == "if" || lowerLex == "while" || lowerLex == "main") { isFollowedByParen = false; }
                                if (isFollowedByParen)
                                {
                                    Errors.Add($"Cannot use keyword '{lexeme}' as function name");
                                    Tokens.Add(new Token { lex = lexeme, token_type = ReservedWords[upperLex], description = desc });
                                }
                                else
                                {
                                    Tokens.Add(new Token { lex = lexeme, token_type = ReservedWords[upperLex], description = desc });

                                    if (new[] { "repeat", "if", "elseif", "else", "end", "until", "then" }.Contains(lowerLex))
                                        lineIsControlKeyword = true;
                                }
                            }
                            else
                            {
                                // Keyword with wrong casing becomes identifier + error
                                Tokens.Add(new Token { lex = lexeme, token_type = Token_Class.Identifier, description = "Identifier" });
                                Errors.Add($"Missing semicolon at end of line: '{line}'");
                            }
                        }

                        // --- Handle function declarations and identifiers ---
                        else if (isValidIdentifier)
                        {
                            if (isFollowedByParen)
                            {
                                // --- New logic: Only throw error if both are declared with datatype ---
                                string beforeFunc = line.Substring(0, line.IndexOf(lexeme)).TrimEnd();
                                string[] parts = beforeFunc.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                string possibleType = parts.Length > 0 ? parts[parts.Length - 1].ToLower() : "";
                                bool hasDatatype = possibleType == "int" || possibleType == "float" || possibleType == "string";

                                bool functionAlreadyDeclaredWithType = DeclaredFunctions.Contains(lowerLex) && FunctionsWithDatatypes.Contains(lowerLex);
                                // Extract what's inside the parentheses
                                int startIndex = line.IndexOf('(');
                                int endIndex = line.IndexOf(')', startIndex + 1);

                                if (startIndex != -1 && endIndex != -1)
                                {
                                    string paramList = line.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
                                    if (!string.IsNullOrEmpty(paramList))
                                    {
                                        string[] tokens = Regex.Split(paramList, @"\s+|(?=,)|(?<=,)");

                                        // Remove empty entries and spaces
                                        tokens = tokens.Where(t => !string.IsNullOrWhiteSpace(t)).ToArray();
                                        bool wrongParameters = false;

                                        for (int j = 0; j < tokens.Length - 1; j++)
                                        {
                                            if (!char.IsLetterOrDigit(tokens[j][0]) && tokens[j] != ",")
                                            {
                                                Errors.Add($"Wrong parameters in: '{paramList}'");
                                                wrongParameters = true;
                                                break;
                                            }

                                            if (tokens[j] == "," && tokens[j + 1] == ",")
                                            {
                                                Errors.Add($"More than one comma between parameters in: '{paramList}'");
                                                j++;
                                                while (j < tokens.Length && tokens[j] == ",")
                                                {
                                                    tokens = tokens.Where((item, index) => index != j).ToArray();
                                                    j++;
                                                }
                                            }
                                        }

                                        if (tokens.Length > 0 && tokens[tokens.Length - 1] == ",")
                                        {
                                            Errors.Add($"Extra comma at the end in: '{paramList}'");
                                        }
                                        else if (tokens.Length > 0 && !char.IsLetterOrDigit(tokens[tokens.Length - 1][0]))
                                        {
                                            Errors.Add($"Wrong parameters in: '{paramList}'");
                                        }

                                        if (!wrongParameters)
                                        {
                                            for (int j = 0; j < tokens.Length - 1; j++)
                                            {
                                                if (tokens[j] == "int" ||
                                                    tokens[j] == "string" ||
                                                    tokens[j] == "float" ||
                                                    tokens[j] == ",")
                                                {
                                                    continue;
                                                }

                                                if (tokens[j + 1] != ",")
                                                {
                                                    Errors.Add($"Missing comma between parameters in: '{paramList}'");
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }

                                if (hasDatatype)
                                {
                                    // Check if '{' is in the same line after ')'
                                    int closeParenIndex = line.IndexOf(')', line.IndexOf('('));
                                    bool hasCurlyBraceAfter = closeParenIndex != -1 &&
                                                              line.IndexOf('{', closeParenIndex) != -1;

                                    // If not found in same line, try checking the next line (if available)
                                    if (!hasCurlyBraceAfter && lineIndex + 1 < lines.Length)
                                    {
                                        string nextLine = lines[lineIndex + 1].Trim();
                                        hasCurlyBraceAfter = nextLine.StartsWith("{");
                                    }

                                    if (!hasCurlyBraceAfter)
                                    {
                                        Errors.Add($"Function '{lexeme}' must have a body enclosed in '{{}}'");
                                    }
                                    if (DeclaredFunctions.Contains(lowerLex))
                                    {
                                        if (lowerLex == "main")
                                            Errors.Add("Only one main function can exist");
                                        else if (functionAlreadyDeclaredWithType)
                                            Errors.Add($"Function of same name already exists: '{lexeme}'");
                                    }
                                    else
                                    {
                                        DeclaredFunctions.Add(lowerLex);
                                        FunctionsWithDatatypes.Add(lowerLex);
                                        Tokens.Add(new Token { lex = lexeme, token_type = Token_Class.Function, description = "Function" });
                                    }
                                    isFunctionHeader = true;

                                }
                                else
                                {

                                    Tokens.Add(new Token { lex = lexeme, token_type = Token_Class.Function, description = "Function Call" });
                                    hasBracket = false;

                                }

                            }
                            else
                            {
                                Tokens.Add(new Token { lex = lexeme, token_type = Token_Class.Identifier, description = "Identifier" });

                                if (lowerLex == "main" && !endsWithSemicolon)
                                    Errors.Add("Missing semicolon after 'main' identifier");
                            }
                        }
                        else
                        {
                            Errors.Add($"Invalid identifier: '{lexeme}'");
                        }

                        continue;
                    }



                    // ----------- Unrecognized character -----------
                    Errors.Add($"Unrecognized character: '{currentChar}'");
                    i++;
                }

                // ----------- Semicolon Check -----------
                string trimmed = line.TrimEnd();
                if (!endsWithSemicolon &&
                    !lineIsControlKeyword &&
                    !isComment &&
                    !isFunctionHeader &&
                    !hasBrace &&
                    !hasBracket &&
                    !NonSemicolonEndKeywords.Any(k => trimmed.ToUpper().StartsWith(k)))
                {
                    Errors.Add($"Missing semicolon at end of line: '{trimmed}'");
                }
            }

            if (openCurlyCount > 0)
            {
                Errors.Add("Missing closing brace '}'");
            }
            else if (openCurlyCount < 0)
            {
                Errors.Add("Unmatched closing brace '}'");
            }

            if (openBracketCount > 0)
            {
                Errors.Add("Missing closing bracket ')'");
            }
            else if (openBracketCount < 0)
            {
                Errors.Add("Unmatched closing bracket ')'");
            }

            JASON_Compiler.TokenStream.AddRange(Tokens);
        }



        public void FindTokenClass(string lexeme)
        {
            Token token = new Token { lex = lexeme };

            if (ReservedWords.ContainsKey(lexeme.ToUpper()))
                token.token_type = ReservedWords[lexeme.ToUpper()];
            else if (Operators.ContainsKey(lexeme))
                token.token_type = Operators[lexeme];
            else if (Delimiters.ContainsKey(lexeme))
                token.token_type = Delimiters[lexeme];
            else if (lexeme == ".")
                token.token_type = Token_Class.Dot;
            else if (IsConstant(lexeme))
                token.token_type = Token_Class.Constant;
            else if (IsIdentifier(lexeme))
                token.token_type = Token_Class.Identifier;
            else
            {
                token.token_type = Token_Class.Undefined;
                Errors.Add($"Unrecognized token: '{lexeme}' (This is not a valid identifier, keyword, or operator.)");
            }

            if (token.token_type != Token_Class.Undefined)
                Tokens.Add(token);
        }

        private bool IsIdentifier(string lexeme) => Regex.IsMatch(lexeme, @"^[A-Za-z_][A-Za-z0-9_]*$");

        private bool IsConstant(string lexeme) => Regex.IsMatch(lexeme, @"^\d+(\.\d+)?$");
    }
}