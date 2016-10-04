using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;

namespace Jarvis
{
  public class Utilities
  {
    public static List<TestCase> ReadTestCases(string path)
    {
      List<TestCase> testCases = new List<TestCase>();
      TestCase currentTest = null;
      int id = 0;

      if (File.Exists(path))
      {        
        FileStream stream = File.OpenRead(path);

        using (XmlReader reader = XmlReader.Create(stream))
        {
          while (reader.Read())
          {
            switch (reader.NodeType)
            {
              case XmlNodeType.Element:
                switch (reader.Name)
                {
                  case "config":
                    // Do nothing
                    break;

                  case "test":
                    if (currentTest != null)
                    {
                      testCases.Add(currentTest);
                    }

                    currentTest = new TestCase(id);
                    ++id;
                    break;

                  case "stdin":
                    currentTest.StdInputFile = reader.GetAttribute("file");
                    break;

                  case "stdout":
                    currentTest.StdOutputFile = reader.GetAttribute("file");
                    break;

                  case "filein":
                    {
                      string courseFile = reader.GetAttribute("courseFile");
                      string studentFile = reader.GetAttribute("studentFile");
                      currentTest.FileInputFiles.Add(new Tuple<string,string>(courseFile, studentFile));
                    }
                    break;

                  case "fileout":
                    {
                      string courseFile = reader.GetAttribute("courseFile");
                      string studentFile = reader.GetAttribute("studentFile");
                      currentTest.FileOutputFiles.Add(new Tuple<string,string>(courseFile, studentFile));
                    }
                    break;
                }
                break;
            }
          }
        }

        if (currentTest != null)
        {
          testCases.Add(currentTest);
        }

        stream.Close();
        stream.Dispose();
      }

      return testCases;
    }

    public static string ReadFileContents(string path)
    {
      StreamReader reader = new StreamReader(path);

      return reader.ReadToEnd();
    }

    public static string ToHtmlEncoding(string text)
    {
      text = text.Replace("<", "&lt;");
      text = text.Replace(">", "&gt;");
      text = text.Replace(" ", "&nbsp;");
      text = text.Replace("\n", "<br />");

      return text;
    }

    public static string ToHtmlEncodingWithNewLines(string text)
    {
      text = text.Replace("<", "&lt;");
      text = text.Replace(">", "&gt;");
      text = text.Replace(" ", "&nbsp;");
      //Handle ASCII non-printables
      text = text.Replace("\0", "<span style='color: #888888; font-size: 10px;'>\\0</span>");
      text = text.Replace("\a", "<span style='color: #888888; font-size: 10px;'>\\a</span>");
      text = text.Replace("\b", "<span style='color: #888888; font-size: 10px;'>\\b</span>");
      text = text.Replace("\t", "<span style='color: #888888; font-size: 10px;'>\\t&emsp;</span>"); //replace ascii tab with HTML em space (this seemed to give the best results)
      text = text.Replace("\n", "<span style='color: #888888; font-size: 10px;'>\\n</span><br />");
      text = text.Replace("\v", "<span style='color: #888888; font-size: 10px;'>\\v</span>");
      text = text.Replace("\f", "<span style='color: #888888; font-size: 10px;'>\\f</span>");
      text = text.Replace("\r", "<span style='color: #888888; font-size: 10px;'>\\r</span>");

      return text;
    }

    public static string ToTextEncoding(string text)
    {
      text = text.Replace("<", "&lt;");
      text = text.Replace(">", "&gt;");
      text = text.Replace("&nbsp;", " ");
      text = text.Replace("<br />", "\n");

      return text;
    }

    public static void DirectoryCopy(string sourceDirName, string destDirName)
    {
      // Get the subdirectories for the specified directory.
      DirectoryInfo dir = new DirectoryInfo(sourceDirName);

      if (!dir.Exists)
      {
        throw new DirectoryNotFoundException(
          "Source directory does not exist or could not be found: "
          + sourceDirName);
      }

      DirectoryInfo[] dirs = dir.GetDirectories();
      // If the destination directory doesn't exist, create it.
      if (!Directory.Exists(destDirName))
      {
        Directory.CreateDirectory(destDirName);
      }

      // Get the files in the directory and copy them to the new location.
      FileInfo[] files = dir.GetFiles();
      foreach (FileInfo file in files)
      {
        string temppath = Path.Combine(destDirName, file.Name);
        file.CopyTo(temppath, false);
      }

      foreach (DirectoryInfo subdir in dirs)
      {
        string temppath = Path.Combine(destDirName, subdir.Name);
        DirectoryCopy(subdir.FullName, temppath);
      }
    }
  }
}

