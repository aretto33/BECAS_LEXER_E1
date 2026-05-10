using Antlr4.Runtime;
using System;
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
VAR promedio_minimo = 90.5

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
                // ANTLR convierte el texto en tokens y luego en un arbol de sintaxis.
                AntlrInputStream inputStream = new AntlrInputStream(input);
                BecasLexer lexer = new BecasLexer(inputStream);
                CommonTokenStream tokens = new CommonTokenStream(lexer);
                BecasParser parser = new BecasParser(tokens);

                Console.WriteLine(">>> Analizando código...");
                
                // Primero revisa la sintaxis y obtiene el árbol del programa.
                var program = parser.start_rule();

                // Revisamos si ANTLR detectó errores
                if (parser.NumberOfSyntaxErrors == 0)
                {
                    Console.WriteLine(">>> [ÉXITO]: El código es válido.");
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
    }
}
