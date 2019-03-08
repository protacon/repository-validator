using System;
using System.IO;
using System.Linq;
using CsvHelper;
using Microsoft.Extensions.Logging;
using ValidationLibrary;

namespace ValidationLibrary.Csv
{
    public class CsvReporter
    {
        private ILogger _logger;
        private readonly FileInfo _destinationFile;

        public CsvReporter(ILogger logger, FileInfo destinationFile)
        {
            _logger = logger;
            _destinationFile = destinationFile;
        }

        public void Report(params ValidationReport[] reports)
        {
            _logger.LogTrace("Reporting {0} reports to CSV {1}", reports.Count(), _destinationFile.FullName);
            var flatten = from report in reports
                          from result in report.Results
                          select new ReportLine {
                            Owner = report.Owner,
                            Name = report.RepositoryName,
                            RepositoryUrl = report.RepositoryUrl,
                            RuleName = result.RuleName,
                            IsValid = result.IsValid,
                            HowToFix = result.HowToFix
                          };

            using (var writer = new StreamWriter(_destinationFile.FullName))
            using (var csv = new CsvWriter(writer))
            {    
                csv.WriteRecords(flatten.ToList());
            }  
        }

        private class ReportLine 
        {
            public string Owner { get; set; }
            public string Name { get; set; }
            public string RepositoryUrl { get; set; }
            public string RuleName { get; set; }
            public bool IsValid { get; set; }
            public string HowToFix { get; set; }
        }
    }
}
