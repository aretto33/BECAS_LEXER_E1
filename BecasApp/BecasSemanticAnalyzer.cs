using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BecasApp
{
    class BecasSemanticResult
    {
        public BecasSemanticResult(bool isValid, string report)
        {
            IsValid = isValid;
            Report = report;
        }

        public bool IsValid { get; }
        public string Report { get; }
    }

    class BecasSemanticAnalyzer
    {
        private readonly HashSet<string> variables = new HashSet<string>();
        private readonly List<string> reportLines = new List<string>();
        private readonly List<string> errors = new List<string>();

        public BecasSemanticResult Analyze(BecasParser.Start_ruleContext program)
        {
            variables.Clear();
            reportLines.Clear();
            errors.Clear();

            AnalyzeInstructions(program.instrucciones());

            if (errors.Count == 0)
            {
                reportLines.Add("Resultado: analisis semantico correcto.");
            }
            else
            {
                reportLines.Add("Resultado: analisis semantico con errores.");
                foreach (string error in errors)
                {
                    reportLines.Add($"ERROR: {error}");
                }
            }

            return new BecasSemanticResult(errors.Count == 0, string.Join("\n", reportLines));
        }

        private void AnalyzeInstructions(BecasParser.InstruccionesContext instructions)
        {
            foreach (var instruction in instructions.instruccion())
            {
                AnalyzeInstruction(instruction);
            }
        }

        private void AnalyzeInstruction(BecasParser.InstruccionContext instruction)
        {
            if (instruction.var_decl() != null)
            {
                AnalyzeVarDeclaration(instruction.var_decl());
            }
            else if (instruction.input_datos() != null)
            {
                AnalyzeAssignment(instruction.input_datos());
            }
            else if (instruction.input_usuario() != null)
            {
                AnalyzeUserInput(instruction.input_usuario());
            }
            else if (instruction.asignacion_suma() != null)
            {
                AnalyzeSumAssignment(instruction.asignacion_suma());
            }
            else if (instruction.ciclo_loop() != null)
            {
                AnalyzeLoop(instruction.ciclo_loop());
            }
            else if (instruction.check_block() != null)
            {
                AnalyzeCheck(instruction.check_block());
            }
            else if (instruction.salida() != null)
            {
                AnalyzeOutput(instruction.salida());
            }
        }

        private void AnalyzeVarDeclaration(BecasParser.Var_declContext declaration)
        {
            string value = declaration.numero().GetText();
            foreach (var id in declaration.ID())
            {
                variables.Add(id.GetText());
            }

            reportLines.Add($"Declaracion valida: {string.Join(", ", declaration.ID().Select(id => id.GetText()))} = {value}");
        }

        private void AnalyzeAssignment(BecasParser.Input_datosContext assignment)
        {
            string name = assignment.ID().GetText();
            variables.Add(name);
            reportLines.Add($"Asignacion valida: {name} recibe el numero {assignment.numero().GetText()}");
        }

        private void AnalyzeUserInput(BecasParser.Input_usuarioContext input)
        {
            string name = input.ID().GetText();
            variables.Add(name);
            reportLines.Add($"Entrada valida: INPUT guarda un numero en {name}");
        }

        private void AnalyzeSumAssignment(BecasParser.Asignacion_sumaContext assignment)
        {
            string target = assignment.ID().GetText();
            ValidateExpression(assignment.exp(0));
            ValidateExpression(assignment.exp(1));
            variables.Add(target);
            reportLines.Add($"Suma valida: {target} recibe {assignment.exp(0).GetText()} + {assignment.exp(1).GetText()}");
        }

        private void AnalyzeLoop(BecasParser.Ciclo_loopContext loop)
        {
            string iterator = loop.ID().GetText();
            int repetitions = int.Parse(loop.INT().GetText(), CultureInfo.InvariantCulture);
            variables.Add(iterator);

            reportLines.Add($"Ciclo valido: {iterator} se usa como iterador en range({repetitions})");
            AnalyzeInstructions(loop.instrucciones());
        }

        private void AnalyzeCheck(BecasParser.Check_blockContext check)
        {
            ValidateCondition(check.condicion());
            reportLines.Add("Condicional valido: IF contiene una condicion booleana");
            AnalyzeInstructions(check.instrucciones());

            foreach (var elseifBlock in check.elseif_bloque())
            {
                ValidateCondition(elseifBlock.condicion());
                reportLines.Add("Condicional valido: ELSEIF contiene una condicion booleana");
                AnalyzeInstructions(elseifBlock.instrucciones());
            }

            if (check.else_bloque() != null)
            {
                reportLines.Add("Condicional valido: ELSE no requiere condicion");
                AnalyzeInstructions(check.else_bloque().instrucciones());
            }
        }

        private void AnalyzeOutput(BecasParser.SalidaContext output)
        {
            foreach (var value in output.valor_salida())
            {
                if (value.ID() != null)
                {
                    ValidateVariableExists(value.ID().GetText());
                }
            }

            reportLines.Add($"Salida valida: OUT imprime {output.GetText()}");
        }

        private void ValidateCondition(BecasParser.CondicionContext condition)
        {
            foreach (var comparison in FindComparisons(condition))
            {
                ValidateExpression(comparison.exp(0));
                ValidateExpression(comparison.exp(1));
                reportLines.Add($"Comparacion valida: {comparison.exp(0).GetText()} {comparison.OP_REL().GetText()} {comparison.exp(1).GetText()}");
            }
        }

        private IEnumerable<BecasParser.ComparacionContext> FindComparisons(BecasParser.CondicionContext condition)
        {
            return condition.or_cond()
                .and_cond()
                .SelectMany(andCondition => andCondition.not_cond())
                .SelectMany(FindComparisons);
        }

        private IEnumerable<BecasParser.ComparacionContext> FindComparisons(BecasParser.Not_condContext condition)
        {
            if (condition.comparacion() != null)
            {
                yield return condition.comparacion();
            }
            else if (condition.condicion() != null)
            {
                foreach (var comparison in FindComparisons(condition.condicion()))
                {
                    yield return comparison;
                }
            }
            else if (condition.not_cond() != null)
            {
                foreach (var comparison in FindComparisons(condition.not_cond()))
                {
                    yield return comparison;
                }
            }
        }

        private void ValidateExpression(BecasParser.ExpContext expression)
        {
            if (expression.ID() != null)
            {
                ValidateVariableExists(expression.ID().GetText());
            }
        }

        private void ValidateVariableExists(string name)
        {
            if (!variables.Contains(name))
            {
                errors.Add($"La variable '{name}' se usa antes de tener un valor.");
            }
        }
    }
}
