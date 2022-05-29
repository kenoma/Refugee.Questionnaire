using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using RQ.DTO;

try
{
    var config = new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        PrepareHeaderForMatch = args => args.Header.ToLower(),
        MissingFieldFound = null,
        IgnoreBlankLines = true,
        BadDataFound = null,
        DetectDelimiter = true,
        DetectDelimiterValues = new[] { ",", ";", "\t" }
    };

    using var reader = new StreamReader(args[0]);
    using var csv = new CsvReader(reader, config);
    var _ = csv.GetRecords<QuestionnaireEntry>().ToArray();
    Console.WriteLine("OK");
    Environment.Exit(0);
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    Environment.Exit(1);
}
