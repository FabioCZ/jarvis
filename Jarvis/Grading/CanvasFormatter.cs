using System;
using CsvHelper;
using System.Collections.Generic;
using System.IO;
using System.Dynamic;

namespace Jarvis
{
  public class CanvasFormatter
  {
    private const int STUDENT_NAME = 0;
    private const int CANVAS_ID    = 1; 
    private const int SIS_USER_ID  = 2;
    private const int STUDENT_ID   = 3;
    private const int SECTION      = 5;

    public string GenerateCanvasCsv(string csvPath, string homeworkId, List<GradingResult> results)
    {            
      // open course csv
      string gradesPath = string.Format("{0}/grades_hw{1}.csv", csvPath, homeworkId);
      CsvReader reader = new CsvReader(new StreamReader(File.OpenRead(csvPath + "../canvas.csv")));
      CsvWriter writer = new CsvWriter(new StreamWriter(File.OpenWrite(gradesPath)));

      reader.ReadHeader();

      writer.WriteField<string>(reader.FieldHeaders[STUDENT_NAME]);
      writer.WriteField<string>(reader.FieldHeaders[CANVAS_ID]);
      writer.WriteField<string>(reader.FieldHeaders[SIS_USER_ID]);
      writer.WriteField<string>(reader.FieldHeaders[STUDENT_ID]);
      writer.WriteField<string>(reader.FieldHeaders[SECTION]);

      for (int i = 0; i < reader.FieldHeaders.Length; ++i)
      {        
        if (reader.FieldHeaders[i].Contains("HW" + homeworkId))
        {
          writer.WriteField(reader.FieldHeaders[i]);
          writer.NextRecord();
          break;
        }
      }
        
      reader.Read(); // skip the points possible line
      while (reader.Read())
      {
        // copy all required columns
        string studentName = reader.GetField<string>(reader.FieldHeaders[STUDENT_NAME]);
        string canvasId = reader.GetField<string>(reader.FieldHeaders[CANVAS_ID]);
        string sisUserId = reader.GetField<string>(reader.FieldHeaders[SIS_USER_ID]);          
        string studentId = reader.GetField<string>(reader.FieldHeaders[STUDENT_ID]);
        string section = reader.GetField<string>(reader.FieldHeaders[SECTION]);

        writer.WriteField<string>(studentName);
        writer.WriteField<string>(canvasId);
        writer.WriteField<string>(sisUserId);
        writer.WriteField<string>(studentId);
        writer.WriteField<string>(section);

        // find students grade and write it
        foreach(GradingResult result in results)
        {
          if (result.Assignment.StudentId.Equals(studentId, StringComparison.OrdinalIgnoreCase))
          {
            writer.WriteField<string>(result.Grade.ToString());
          }  
        }
        
        writer.NextRecord();
      }                

      reader.Dispose();
      writer.Dispose();

      return gradesPath;
    }
  }
}

