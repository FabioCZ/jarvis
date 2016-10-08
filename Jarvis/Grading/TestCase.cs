using System;
using System.Collections.Generic;
using System.Text;

namespace Jarvis
{
  public class TestCase
  {
    public int Id { get; set; }
    public string StdInputFile { get; set; }
    public string StdOutputFile { get; set; }

    public List<Tuple<string,string>> FileInputFiles { get; set; }
    public List<Tuple<string,string>> FileOutputFiles { get; set; }

    public bool Passed { get; set; }

    public List<string> DiffBlocks { get; set; }

    public string Results 
    { 
      get
      {
        StringBuilder result = new StringBuilder();

        string passedText = Passed ? "<span style=\"color:#00ff00\">Passed</span>" : "<span style=\"color:#ff0000\">Failed</span>";

        result.Append("<p style='display: inline;'>------------------------------------------------------------------</p>");
        result.Append("<h3 style='margin-top: 0px; margin-bottom: 0px;'>Test case " + Id.ToString() + ": " + passedText + "</h3>");
        result.Append("<p style='display: inline;'>------------------------------------------------------------------</p>");

        foreach (string diff in DiffBlocks)
        {
          result.AppendLine(diff);
        }

        result.AppendLine("<br />");
        result.AppendLine("<br />");
        return result.ToString();
      }
    }


    public TestCase(int id)
    {
      Id = id;

      Passed = true;
      FileInputFiles = new List<Tuple<string, string>>();
      FileOutputFiles = new List<Tuple<string, string>>();
      DiffBlocks = new List<string>();
    }
  }
}
