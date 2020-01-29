using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using Microsoft.Extensions.Logging;

namespace ValidationLibrary.Csv
{
    public class CsvReporter
    {
        private readonly ILogger _logger;
        private readonly FileInfo _destinationFile;

        public CsvReporter(ILogger logger, FileInfo destinationFile)
        {
            _logger = logger;
            _destinationFile = destinationFile;
        }

        public void Report(IEnumerable<ValidationReport> reports)
        {
            _logger.LogTrace("Reporting {count} reports to CSV {destinationFile}", reports.Count(), _destinationFile.FullName);
            var flatten = from report in reports
                          from result in report.Results
                          select new ReportLine
                          {
                              Timestamp = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffK"),
                              Owner = report.Owner,
                              Name = report.RepositoryName,
                              RepositoryUrl = report.RepositoryUrl,
                              RuleName = result.RuleName,
                              IsValid = result.IsValid,
                              HowToFix = result.HowToFix
                          };

            using (var writer = new StreamWriter(_destinationFile.FullName, true))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(flatten.ToList());
            }
        }

        private class ReportLine
        {
            public string Timestamp { get; set; }
            public string Owner { get; set; }
            public string Name { get; set; }
            public string RepositoryUrl { get; set; }
            public string RuleName { get; set; }
            public bool IsValid { get; set; }
            public string HowToFix { get; set; }
        }
    }
}
