using System;
using System.IO;
using System.Collections.Generic;
using Xunit;
using LogicExpressions;
using Input;
using TruthTable;

namespace TruthTableTests;

// Testklasse für CLI-Eingaben des Nutzers
public class CLIIntegrationstests
{
    [Fact]
    public void MainMitKommandos()
    {
        string[] args = { "tabelle" };
        string simulierterInput = "A AND B\n";
        using var stringLeser = new StringReader(simulierterInput);
        using var stringSchreiber = new StringWriter();

        var originalerInput = Console.In;
        var originalerOutput = Console.Out;

        try
        {
            Console.SetIn(stringLeser);
            Console.SetOut(stringSchreiber);

            // Ausführen von Main() mit den Testargumenten -> Funktionalitätsprüfung bei vollständ. 
            // Eingabe
            Program.Main(args);

            string ausgabe = stringSchreiber.ToString();
            Assert.NotEmpty(ausgabe);
            Assert.Contains("Enter a logical expression", ausgabe);
        }
        finally
        {
            Console.SetIn(originalerInput);
            Console.SetOut(originalerOutput);
        }
    }

    [Fact]
    public void MainKeineArgumenteUebergeben()
    {
        string[] args = Array.Empty<string>();
        using var stringSchreiber = new StringWriter();
        var originalerOutput = Console.Out;

        try
        {
            Console.SetOut(stringSchreiber);

            // Aufruf von Main(), diesmal mit leerem Argument-String
            Program.Main(args);

            string ausgabe = stringSchreiber.ToString();
            Assert.Contains("Usage:", ausgabe);
            Assert.Contains("dotnet run tabelle", ausgabe);
        }
        finally
        {
            Console.SetOut(originalerOutput);
        }
    }

    [Fact]
    public void Main_VollstaendigerWorkflow_PrueftWahrheitsmatrix()
    {
        string[] args = { "tabelle" };
        // Übergabe eines richtigen Ausdrucks und Testen des gesamten Workflows damit
        string simulierterInput = "A AND B\n";
        using var stringLeser = new StringReader(simulierterInput);
        using var stringSchreiber = new StringWriter();

        var originalerInput = Console.In;
        var originalerOutput = Console.Out;

        try
        {
            Console.SetIn(stringLeser);
            Console.SetOut(stringSchreiber);

            Program.Main(args);

            string ausgabe = stringSchreiber.ToString();

            Assert.Contains("A", ausgabe);
            Assert.Contains("B", ausgabe);
            Assert.Contains("Result", ausgabe);

            // Writer gibt 0/1 aus (GetDisplayValue) -> A | B | Wert je Spalte
            Assert.Contains("1 | 1 | 1", ausgabe); // A=1, B=1 -> A AND B = 1
            Assert.Contains("1 | 0 | 0", ausgabe); // A=1, B=0 -> A AND B = 0
        }
        finally
        {
            Console.SetIn(originalerInput);
            Console.SetOut(originalerOutput);
        }
    }
}

// Unittests für log. Operatoren, Klammerpriorität
public class OperatorKlammerTests
{
    [Theory]
    // AND-Testdaten
    [InlineData(LogicOperator.And, true, true, true)]
    [InlineData(LogicOperator.And, true, false, false)]
    // OR-Testdaten
    [InlineData(LogicOperator.Or, false, true, true)]
    [InlineData(LogicOperator.Or, false, false, false)]
    public void KorrekteBerechnungPruefen(LogicOperator op, bool a, bool b, bool erwartung)
    {
        var WertA = new Variable("A");
        var WertB = new Variable("B");

        // dynamisches Erstellen der richtigen Prüfbedingung je nach Operator
        var klausel = new LogicClause(op, WertA, WertB);
        var zuweisungen = new Dictionary<string, bool> { { "A", a }, { "B", b } };

        bool ergebnis = TruthExpressionEvaluator.Evaluate(klausel, zuweisungen);

        Assert.Equal(erwartung, ergebnis);
    }

    // Unit-Test für Not
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void KorrekteBerechnungNotPruefen(bool input, bool erwartung)
    {
        var wertA = new Variable("A");
        var klausel = new LogicClause(LogicOperator.Not, wertA, null);
        var zuweisungen = new Dictionary<string, bool> { { "A", input } };

        bool ergebnis = TruthExpressionEvaluator.Evaluate(klausel, zuweisungen);

        Assert.Equal(erwartung, ergebnis);
    }

    // Klammern: Prioritätsänderungen durch diese möglich
    [Fact]
    public void KlammerprioritaetUeberschreibtPraezedenz()
    {
        var inputModule = new TruthTermInputModule();

        // A=false, B=true, C=false
        var zuweisungen = new Dictionary<string, bool> { { "A", false }, { "B", true }, { "C", false } };

        // Ohne Klammern: A OR (B AND C) = false OR (true AND false) = false
        var ohneKlammern = inputModule.Parse("A OR B AND C");
        bool ergebnisOhne = TruthExpressionEvaluator.Evaluate(ohneKlammern.RootClause, zuweisungen);
        Assert.False(ergebnisOhne);

        // Mit Klammern: (A OR B) AND C = (false OR true) AND false = false
        // -> Gegenprobe mit C=true als Unterscheidungstest
        var zuweisungenC = new Dictionary<string, bool> { { "A", false }, { "B", true }, { "C", true } };

        var mitKlammern = inputModule.Parse("(A OR B) AND C");
        bool ergebnisMit = TruthExpressionEvaluator.Evaluate(mitKlammern.RootClause, zuweisungenC);
        Assert.True(ergebnisMit); // (false OR true) AND true = true

        var ohneKlammern2 = inputModule.Parse("A OR B AND C");
        bool ergebnisOhne2 = TruthExpressionEvaluator.Evaluate(ohneKlammern2.RootClause, zuweisungenC);
        Assert.True(ergebnisOhne2); // false OR (true AND true) = true

        // Hier: kritischer Fall, dessen Ergebnis von der Klammerung abhängt
        var zuweisungenDiff = new Dictionary<string, bool> { { "A", true }, { "B", false }, { "C", false } };

        // A OR B AND C = true OR (false AND false) = true
        var expr1 = inputModule.Parse("A OR B AND C");
        Assert.True(TruthExpressionEvaluator.Evaluate(expr1.RootClause, zuweisungenDiff));

        // (A OR B) AND C = (true OR false) AND false = false
        var expr2 = inputModule.Parse("(A OR B) AND C");
        Assert.False(TruthExpressionEvaluator.Evaluate(expr2.RootClause, zuweisungenDiff));
    }

    // Validierung der Matrixgröße: 2^n Zeilen bei n Variablen
    [Theory]
    [InlineData("A", 2)]              // 2^1
    [InlineData("A AND B", 4)]        // 2^2
    [InlineData("A AND B OR C", 8)]   // 2^3
    public void MatrixHat2HochNZeilen(string ausdruck, int erwarteteZeilen)
    {
        var inputModule = new TruthTermInputModule();
        var truthTerm = inputModule.Parse(ausdruck);

        TruthTableMatrix matrix = TruthTableMatrixGenerator.Generate(truthTerm);

        Assert.Equal(erwarteteZeilen, matrix.Rows.Count);
    }
}

// Parser-Fehlertests: ungültige bzw. unvollständige Eingaben
public class ParserFehlerTests
{
    // Leere Eingabe -> ArgumentException ("Unexpected end of expression")
    [Fact]
    public void LeereEingabe_WirftArgumentException()
    {
        var inputModule = new TruthTermInputModule();

        Assert.Throws<ArgumentException>(() => inputModule.Parse(""));
    }

    // Fehlende schließende Klammer -> ArgumentException ("Missing closing parenthesis")
    [Fact]
    public void FehlendeKlammer_WirftArgumentException()
    {
        var inputModule = new TruthTermInputModule();

        Assert.Throws<ArgumentException>(() => inputModule.Parse("(A AND B"));
    }

    // Ungültiges Zeichen -> ArgumentException ("Invalid character")
    [Fact]
    public void UngueltigesZeichen_WirftArgumentException()
    {
        var inputModule = new TruthTermInputModule();

        Assert.Throws<ArgumentException>(() => inputModule.Parse("A & B"));
    }
}

// Ausschluss nicht implementierter Operatoren -> bereits beim Parsen except. geworfen
public class NichtImplementierteOperatorenTests
{
    [Theory]
    [InlineData("A XOR B")]
    [InlineData("A NAND B")]
    [InlineData("A NOR B")]
    public void NichtImplementierteOperatoren_WerfenNotImplementedException(string ausdruck)
    {
        var inputModule = new TruthTermInputModule();

        Assert.Throws<NotImplementedException>(() => inputModule.Parse(ausdruck));
    }
}