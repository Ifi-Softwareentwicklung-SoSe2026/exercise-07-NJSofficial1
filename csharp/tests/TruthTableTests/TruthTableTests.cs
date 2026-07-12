using System;
using System.IO;
using System.Reflection;
using Xunit;
using LogicExpressions;
using Input;
using TruthTable;
using System.Security.Cryptography.X509Certificates;

namespace TruthTableTests;

// Testklasse für CLI-Eingaben des Nutzers
public class CLIIntegrationstests
{
    [Fact]
    public void MainMitKommandos()
    {
        string [] args = { "tabelle" };
        string simulierterInput = "A AND B\n";
        using var stringLeser = new StringReader(simulierterInput);
        using var stringSchreiber = new StringWriter();

        var originalerInput = Console.In;
        var originalerOutput = Console.Out;

        try
        {
            Console.SetIn(stringLeser);
            Console.SetOut(stringSchreiber);

            // Ausführen von Main() mit den Testargumenten -> Funktionalitätsprüfung
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
}

// Unittests für log. Operatoren, Klammerpriorität
public class OperatorKlammerTests
{
    [Theory]
    // AND-Testdaten
    [InlineData(LogicOperator.And, true, true, true)]
    [InlineData(LogicOperator.And, true, false, false)]
    // OR-Testdatem
    [InlineData(LogicOperator.Or, false, true, true)]
    [InlineData(LogicOperator.Or, false, false, false)]
    public void KorrekteBerechnungPruefen(LogicOperator op, bool a, bool b, bool erwartung)
    {
        var WertA = new Variable("A"); // Hinweis: "new Variable", nicht "newVariable"
        var WertB = new Variable("B");
        
        // dynamisches Erstellen der richtigen Prüfbedingung je nach Operator
        var klausel = new LogicClause(op, WertA, WertB);
        var zuweisungen = new Dictionary<string, bool> { { "A", a }, { "B", b } };

        bool ergebnis = TruthExpressionEvaluator.Evaluate(klausel, zuweisungen);

        Assert.Equal(erwartung, ergebnis);
    }
}
