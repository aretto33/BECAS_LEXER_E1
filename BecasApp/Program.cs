using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.IO;

namespace BecasApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Si se pasa un archivo en la terminal, ejecuta ese archivo.
            // Si no se pasa nada, usa el ejemplo incluido abajo.
            string input = args.Length > 0
                ? File.ReadAllText(args[0])
                : @"STAR
//crear variables de los contadores
VAR aceptados, rechazado_edad, rechazado_prom, rechazado_ingreso = 0
VAR promedio_minimo = 80.5

LOOP i IN range (5):
    OUT ""Bienvenido alumno No."" + i
    INPUT ""Edad: "" edad
    INPUT ""Promedio: "" promedio
    INPUT ""Ingreso: "" ingreso
    CHECK:
        IF (!(edad <= 18) && !(edad >= 25) && promedio >= promedio_minimo && !(ingreso >= 5000)) SO:
            aceptados = aceptados + 1
            OUT ""Beca aceptada con promedio "" + promedio
        ELSEIF (edad <= 18 || edad >= 25) SO:
            rechazado_edad = rechazado_edad + 1
            OUT ""Rechazado: EDAD""
        ELSEIF (ingreso >= 5000) SO:
            rechazado_ingreso = rechazado_ingreso + 1
            OUT ""Rechazado: INGRESO""
        ELSE:
            rechazado_prom = rechazado_prom + 1
            OUT ""Rechazado: PROMEDIO""
    END_CHECK
END_LOOP

OUT ""Total: "" + aceptados
END";

            try
            {
                Console.WriteLine("========== ANALISIS LEXICO ==========");
                PrintLexicalAnalysis(input);

                // ANTLR convierte el texto en tokens y luego en un arbol de sintaxis.
                AntlrInputStream inputStream = new AntlrInputStream(input);
                BecasLexer lexer = new BecasLexer(inputStream);
                CommonTokenStream tokens = new CommonTokenStream(lexer);
                BecasParser parser = new BecasParser(tokens);

                Console.WriteLine();
                Console.WriteLine("========== ANALISIS SINTACTICO / GRAMATICA ==========");
                
                // Primero revisa la sintaxis y obtiene el árbol del programa.
                var program = parser.start_rule();

                // Revisamos si ANTLR detectó errores
                if (parser.NumberOfSyntaxErrors == 0)
                {
                    Console.WriteLine(">>> [EXITO]: El codigo cumple con la gramatica.");
                    PrintGrammarAnalysis(program, parser);

                    Console.WriteLine();
                    Console.WriteLine("========== ANALISIS SEMANTICO ==========");
                    var semanticAnalyzer = new BecasSemanticAnalyzer();
                    var semanticResult = semanticAnalyzer.Analyze(program);
                    Console.WriteLine(semanticResult.Report);

                    if (!semanticResult.IsValid)
                    {
                        Console.WriteLine(">>> [ERROR]: El codigo tiene errores semanticos. No se ejecutara.");
                        return;
                    }

                    Console.WriteLine();
                    Console.WriteLine("========== EJECUCION ==========");
                    Console.WriteLine(">>> Ejecutando programa...");

                    // Si no hubo errores, el interprete recorre el arbol y ejecuta el programa.
                    var interpreter = new BecasInterpreter();
                    interpreter.Execute(program);
                }
                else
                {
                    Console.WriteLine($">>> [ERROR]: Se encontraron {parser.NumberOfSyntaxErrors} errores de sintaxis.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(">>> [ERROR CRÍTICO]: " + ex.Message);
            }
        }

        private static void PrintLexicalAnalysis(string input)
        {
            var lexer = new BecasLexer(new AntlrInputStream(input));
            var tokenNames = new Dictionary<int, string>();

            IToken token;
            while ((token = lexer.NextToken()).Type != TokenConstants.EOF)
            {
                string name = tokenNames.TryGetValue(token.Type, out string? cachedName)
                    ? cachedName
                    : GetTokenName(lexer.Vocabulary, token.Type);

                tokenNames[token.Type] = name;
                Console.WriteLine($"Linea {token.Line,2}, Columna {token.Column,2} | {name,-12} | {CleanTokenText(token.Text)}");
            }
        }

        private static void PrintGrammarAnalysis(BecasParser.Start_ruleContext program, BecasParser parser)
        {
            Console.WriteLine("Regla inicial: start_rule -> START instrucciones END");
            Console.WriteLine("Producciones principales:");
            Console.WriteLine("- var_decl -> VAR ID (, ID)* = numero");
            Console.WriteLine("- input_usuario -> INPUT STRING ID");
            Console.WriteLine("- ciclo_loop -> LOOP ID IN range ( INT ) : instrucciones END_LOOP");
            Console.WriteLine("- check_block -> CHECK : IF ( condicion ) SO : instrucciones ELSEIF* ELSE? END_CHECK");
            Console.WriteLine("- salida -> OUT valor_salida (+ valor_salida)?");
            Console.WriteLine("- condicion -> comparaciones con !, &&, ||");
            Console.WriteLine();
            Console.WriteLine("Instrucciones encontradas:");

            foreach (var instruction in program.instrucciones().instruccion())
            {
                Console.WriteLine($"- {GetInstructionName(instruction)}");
            }

            Console.WriteLine();
            Console.WriteLine("Arbol sintactico:");
            Console.WriteLine(program.ToStringTree(parser));
        }

        private static string GetInstructionName(BecasParser.InstruccionContext instruction)
        {
            if (instruction.var_decl() != null) return "declaracion de variables";
            if (instruction.input_datos() != null) return "asignacion numerica";
            if (instruction.input_usuario() != null) return "entrada por teclado";
            if (instruction.ciclo_loop() != null) return "ciclo LOOP";
            if (instruction.check_block() != null) return "bloque CHECK";
            if (instruction.salida() != null) return "salida OUT";
            if (instruction.asignacion_suma() != null) return "asignacion con suma";

            return "instruccion desconocida";
        }

        private static string GetTokenName(IVocabulary vocabulary, int tokenType)
        {
            return vocabulary.GetSymbolicName(tokenType)
                ?? vocabulary.GetLiteralName(tokenType)
                ?? tokenType.ToString();
        }

        private static string CleanTokenText(string text)
        {
            return text
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }
}
