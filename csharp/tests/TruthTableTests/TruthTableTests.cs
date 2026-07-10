using System;
using System.IO;
using System.Reflection;
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

