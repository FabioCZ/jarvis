using System;
using System.Diagnostics;
using System.Text;
using HtmlDiff;
using System.IO;
using System.Timers;
using System.Collections.Generic;
using System.Net.Mail;
using System.IO.Compression;

namespace Jarvis
{
  public class Grader
  {
    public GradingResult Grade(Assignment homework)
    {
      GradingResult result = new GradingResult(homework);

      // Style check
      Logger.Info("Running style check on {0} {1}", homework.StudentId, homework.HomeworkId);
      result.StyleMessage = StyleCheck(homework);

      // Compile
      Logger.Info("Compiling {0} {1}", homework.StudentId, homework.HomeworkId);
      result.CompileMessage = Compile(homework);

      // Run tests
      if (result.CompileMessage == "Success!!")
      {
        Logger.Info("Running {0} {1}", homework.StudentId, homework.HomeworkId);
        result.OutputMessage = RunAllTestCases(homework, result);

        // Delete binary
        File.Delete(homework.Path + homework.StudentId);
      }
      else
      {
        result.OutputMessage = "<p>Didn't compile... :(</p>";
      }

      // Write result into results file, writes a new entry for each run
      RecordResult(homework, result);
      UpdateStats(homework, result);

      return result;
    }

    private void UpdateStats(Assignment homework, GradingResult result)
    {
      AssignmentStats stats = null;
      string name = homework.Course + " - hw" + homework.HomeworkId;
      if (!Jarvis.Stats.AssignmentData.ContainsKey(name))
      {
        stats = new AssignmentStats();
        stats.Name = name;
        Jarvis.Stats.AssignmentData.Add(name, stats);
      }
      else
      {
        stats = Jarvis.Stats.AssignmentData[name];
      }

      stats.TotalSubmissions++;

      if (!stats.TotalUniqueStudentsSubmissions.ContainsKey(homework.StudentId))
      {
        stats.TotalUniqueStudentsSubmissions.Add(homework.StudentId, string.Empty);
      }

      stats.TotalUniqueStudentsSubmissions[homework.StudentId] = result.Grade.ToString();

      if (!result.CompileMessage.Contains("Success!!"))
      {
        stats.TotalNonCompile++;
      }

      if (!result.StyleMessage.Contains("Total&nbsp;errors&nbsp;found:&nbsp;0"))
      {
        stats.TotalBadStyle++;
      }
    }

    private void RecordResult(Assignment homework, GradingResult result)
    {
      string timestamp = DateTime.Now.ToString();

      using (StreamWriter writer = new StreamWriter(homework.Path + "results.txt", true))
      {
        writer.WriteLine(timestamp + " " + homework.StudentId + " " + result.Grade); 
        writer.Flush();
        writer.Close();
      }
    }

    private string StyleCheck (Assignment homework)
    {
      string result = string.Empty;
      using (Process p = new Process())
      {
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;

        string styleExe = Jarvis.Config.AppSettings.Settings["styleExe"].Value;
        p.StartInfo.FileName = styleExe;
        p.StartInfo.Arguments = Jarvis.Config.AppSettings.Settings["styleExemptions"].Value + " " + homework.FullPath;

        Logger.Trace("Style checking with {0} and arguments {1}", styleExe, p.StartInfo.Arguments);

        p.Start();
        Jarvis.StudentProcesses.Add(p.Id);

        result = p.StandardError.ReadToEnd();
        result = result.Replace(homework.Path, "");
        result = Utilities.ToHtmlEncoding(result);
        p.WaitForExit();

        p.Close();
      }

      return result;
    }

    private string Compile(Assignment homework)
    {
      string result = "";
      using (Process p = new Process())
      {
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;

        p.StartInfo.FileName = "g++";
        p.StartInfo.Arguments = "-DJARVIS -std=c++11 -Werror " + homework.FullPath + " -o" + homework.Path + homework.StudentId;
        p.Start();

        Jarvis.StudentProcesses.Add(p.Id);

        result = p.StandardError.ReadToEnd();
        result = result.Replace(homework.Path, "");
        result = Utilities.ToHtmlEncoding(result);

        p.WaitForExit();

        p.Close();
      }
      Logger.Trace("Compile result: {0}", result);

      return (!string.IsNullOrEmpty(result)) ? result : "Success!!";
    }

    private string RunAllTestCases(Assignment homework, GradingResult grade)
    {
      string testsPath = string.Format("{0}/courses/{1}/tests/hw{2}/", Jarvis.Config.AppSettings.Settings["workingDir"].Value, homework.Course, homework.HomeworkId); 

      Logger.Trace("Running tests as configured in {0}", testsPath);
      StringBuilder result = new StringBuilder();
      int passingTestCases = 0;

      if (File.Exists(testsPath + "config.xml"))
      {
        List<TestCase> tests = Utilities.ReadTestCases(testsPath + "config.xml");

        foreach (TestCase test in tests)
        {
          Logger.Info("Running test case {0}", test.Id);
          string stdInput = string.Empty;

          // clear out any previously created input/output files
          DirectoryInfo dir = new DirectoryInfo(homework.Path);
          foreach (FileInfo file in dir.GetFiles())
          {
            if (!file.Name.Contains(homework.StudentId) && !file.Name.Equals("results.txt"))
            {
              file.Delete(); 
            }
          }

          // check for std input file
          if (!string.IsNullOrEmpty(test.StdInputFile))
          {
            stdInput = testsPath + test.StdInputFile;
          }

          // check for file input files
          foreach (Tuple<string,string> filein in test.FileInputFiles)
          {
            File.Copy(testsPath + filein.Item1, homework.Path + filein.Item2, true);
          }

          string actualStdOutput = ExecuteProgram(homework, stdInput);

          // check for std output file
          if (!string.IsNullOrEmpty(test.StdOutputFile))
          {
            string expectedStdOutput = Utilities.ReadFileContents(testsPath + test.StdOutputFile);

            string htmlActualStdOutput = Utilities.ToHtmlEncodingWithNewLines(actualStdOutput);
            string htmlExpectedStdOutput = Utilities.ToHtmlEncodingWithNewLines(expectedStdOutput);
            string htmlDiff = string.Empty;

            if (htmlActualStdOutput.Equals(htmlExpectedStdOutput, StringComparison.Ordinal))
            {
              htmlDiff = "No difference";
            }
            else
            {
              test.Passed = false;
              htmlDiff = HtmlDiff.HtmlDiff.Execute(htmlActualStdOutput, htmlExpectedStdOutput);
            }
            if (string.IsNullOrEmpty(actualStdOutput))
            {
              test.DiffBlocks.Add(BuildDiffBlock("From stdout: <br/> <span style=\"color:#ff0000\">Warning: actual output was empty!</span>", htmlActualStdOutput, htmlExpectedStdOutput, htmlDiff));

            }
            else if (string.IsNullOrWhiteSpace(actualStdOutput))
            {
              test.DiffBlocks.Add(BuildDiffBlock("From stdout: <br/> <span style=\"color:#ff0000\">Warning: actual output was only whitespace!</span>", htmlActualStdOutput, htmlExpectedStdOutput, htmlDiff));

            }
            else
            {
              test.DiffBlocks.Add(BuildDiffBlock("From stdout:", htmlActualStdOutput, htmlExpectedStdOutput, htmlDiff));
            }
          }

          // check for file output files
          if (test.FileOutputFiles.Count > 0)
          {
            foreach (Tuple<string, string> fileout in test.FileOutputFiles)
            {
              string expectedOutput = Utilities.ReadFileContents(testsPath + fileout.Item1);
              FileInfo info = new FileInfo(homework.Path + fileout.Item2);
              if (File.Exists(homework.Path + fileout.Item2) && info.Length < 1000000)
              {
                string actualOutput = Utilities.ReadFileContents(homework.Path + fileout.Item2);

                string htmlExpectedOutput = Utilities.ToHtmlEncodingWithNewLines(expectedOutput);
                string htmlActualOutput = Utilities.ToHtmlEncodingWithNewLines(actualOutput);

                string htmlDiff = string.Empty;

                if (htmlActualOutput.Equals(htmlExpectedOutput, StringComparison.Ordinal))
                {
                  htmlDiff = "No difference";
                }
                else
                {
                  test.Passed = false;
                  htmlDiff = HtmlDiff.HtmlDiff.Execute(htmlActualOutput, htmlExpectedOutput);
                }

                test.DiffBlocks.Add(BuildDiffBlock("From " + fileout.Item2 + ":", htmlActualOutput, htmlExpectedOutput, htmlDiff));
              }
              else if (!File.Exists(homework.Path + fileout.Item2))
              {
                test.Passed = false;
                test.DiffBlocks.Add("<p>Cannot find output file: " + fileout.Item2 + "</p>");
              }
              else if (info.Length >= 1000000)
              {
                test.Passed = false;
                test.DiffBlocks.Add("<p>The file output was too large [" + info.Length.ToString() + "] bytes!!!");
              }
            }
          }

          result.AppendLine(test.Results);

          if (test.Passed)
          {
            passingTestCases++;
          }
        }

        grade.OutputPercentage = passingTestCases / (double)tests.Count;
      }
      else
      {
        result.Append("<p>Sir, I cannot find any test case configurations for this assignment. Perhaps the instructor hasn't set it up yet?<p>");
      }

      return result.ToString();
    }
      
    private string BuildDiffBlock(string source, string htmlActualOutput, string htmlExpectedOutput, string htmlDiff)
    {
      StringBuilder result = new StringBuilder();
      result.Append("<p>" + source + "</p>");
      result.Append("<table>");
      result.Append("<tr>");
      result.Append("<td>");
      result.Append("<h3>Actual</h3>");
      result.Append("<p>" + htmlActualOutput + "</p>");
      result.Append("</td>");
      result.Append("<td>");
      result.Append("<h3>Expected</h3>");
      result.Append("<p>" + htmlExpectedOutput + "</p>");
      result.Append("</td>");
      result.Append("<td>");
      result.Append("<h3>Diff</h3>");
      result.Append("<p>" + htmlDiff + "</p>");
      result.Append("</td>");
      result.Append("</tr>");
      result.Append("</table>");

      return result.ToString();
    }

    private string ExecuteProgram(Assignment homework, string inputFile)
    {      
      string output = string.Empty;
      using (Process executionProcess = new Process())
      {
        executionProcess.StartInfo.WorkingDirectory = homework.Path;
        executionProcess.StartInfo.UseShellExecute = false;
        executionProcess.StartInfo.RedirectStandardOutput = true;
        executionProcess.StartInfo.RedirectStandardError = true;
        executionProcess.StartInfo.RedirectStandardInput = true;

        if (!File.Exists(homework.Path + homework.StudentId))
        {
          Logger.Fatal("Executable " + homework.Path + homework.StudentId + " did not exist!!");
        }

        executionProcess.StartInfo.FileName = homework.Path + homework.StudentId;
        executionProcess.Start();

        Jarvis.StudentProcesses.Add(executionProcess.Id);


        if (File.Exists(inputFile))
        {
          StreamReader reader = new StreamReader(inputFile);

          while (!reader.EndOfStream)
          {
            executionProcess.StandardInput.WriteLine(reader.ReadLine());
          }
        }

        executionProcess.WaitForExit(10000);

        if (executionProcess.HasExited)
        {
          output = executionProcess.StandardOutput.ReadToEnd();
        }
        else
        {
          executionProcess.Kill();
          output = "Sir, the program became unresponsive, either due to an infinite loop or waiting for input.";
        }

        executionProcess.Close();
      }
      
      return output;
    }

    public string GenerateGrades(string baseDir, List<Assignment> assignments)
    {      
      List<GradingResult> gradingResults = new List<GradingResult>();
      // extract to temp directory
      // parse headers
      Logger.Trace("Extracting grader zip file");

      // copy to course directory structure
      string currentHomework = assignments[0].HomeworkId;
      string currentCourse = assignments[0].Course;
      string hwPath = string.Format("{0}/courses/{1}/hw{2}/", baseDir, currentCourse, currentHomework);

      string[] sections = Directory.GetDirectories(hwPath, "section*", SearchOption.AllDirectories);
      foreach (string section in sections)
      {
        if (File.Exists(section + "/grades.txt"))
        {
          File.Delete(section + "/grades.txt");
        }
      }

      Logger.Info("Grading {0} assignments for course: {1} - HW#: {2}", assignments.Count, currentCourse, currentHomework);

      foreach (Assignment a in assignments)
      {
        if (a.ValidHeader)
        {
          string oldPath = a.FullPath;
          a.Path = string.Format("{0}section{1}/{2}/", hwPath, a.Section, a.StudentId);

          Directory.CreateDirectory(a.Path);
          if (File.Exists(a.FullPath))
          {
            File.Delete(a.FullPath);
          }

          Logger.Trace("Moving {0} to {1}", oldPath, a.FullPath);
          File.Move(oldPath, a.FullPath);
        }
      }

      // Check MOSS before grading so we don't have to wait for grading to find out if MOSS fails
      string mossResponse = SendToMoss(hwPath, currentCourse, currentHomework);

      if (string.IsNullOrEmpty(mossResponse))
      {
        return "MOSS Failed!";
      }

      // run grader
      foreach (Assignment a in assignments)
      {     
        if (a.ValidHeader && a.HomeworkId == currentHomework)
        {
          Logger.Trace("Writing grades to {0}", a.Path + "../grades.txt");
          using (StreamWriter writer = File.AppendText(a.Path + "../grades.txt"))
          {
            writer.AutoFlush = true;
            writer.WriteLine("-----------------------------------------------");

            // run grader on each file and save grading result
            Grader grader = new Grader();

            GradingResult result = grader.Grade(a);
            gradingResults.Add(result);
            Logger.Info("Result: {0}", result.Grade);

            string gradingComment = Utilities.ToTextEncoding(result.ToText());

            // write grade to section report              
            writer.WriteLine(string.Format("{0} : {1}", a.StudentId, result.Grade));
            writer.WriteLine(gradingComment);

            writer.Close();
          }
        }
        else
        {
          GradingResult result = new GradingResult(a);
          gradingResults.Add(result);
        }
      }

      // add MOSS URL to result
      string gradingReport = "<a href='" + mossResponse + "'>" + mossResponse + "</a><br />";

      gradingReport += SendFilesToSectionLeaders(hwPath, currentCourse, currentHomework);

      string graderEmail = File.ReadAllText(hwPath + "../grader.txt");

      Logger.Info("Sending Canvas CSV to {0}", graderEmail);

      CanvasFormatter canvasFormatter = new CanvasFormatter();

      string gradesPath = canvasFormatter.GenerateCanvasCsv(hwPath, currentHomework, gradingResults);

      SendEmail(graderEmail,
        "Grades for " + currentCourse + " " + currentHomework,
        "Hello! Attached are the grades for " + currentCourse + " " + currentHomework + ". Happy grading!\n" + mossResponse,
        gradesPath);

      // Generate some kind of grading report
      return gradingReport;
    }

    private string SendToMoss(string hwPath, string currentCourse, string currentHomework)
    {
      // Submit all files to MOSS
      string mossId = Jarvis.Config.AppSettings.Settings["mossId"].Value;

      // Find all *.cpp files in hw directory
      string[] cppFiles = Directory.GetFiles(hwPath, "*.cpp", SearchOption.AllDirectories);

      Logger.Info("Submitting {0} files to MOSS", cppFiles.Length);
      // create moss interface
      MossInterface moss = new MossInterface
      {
        UserId = Int32.Parse(mossId), 
        Language = "cc", // C++
        NumberOfResultsToShow = 500,
        Comments = string.Format("USU - Jarvis - {0} - HW {1}", currentCourse, currentHomework),
      };

      // add files to interface
      moss.Files = new List<string>(cppFiles);

      // submit request
      string mossReponse = string.Empty;
      if (moss.SendRequest(out mossReponse))
      {
        Logger.Info("Moss returned success! {0}", mossReponse);
      }
      else
      {
        mossReponse = "";
        Logger.Warn("Moss submission unsuccessful");
      }

      return mossReponse;
    }


    private void SendEmail(string to, string subject, string body, string attachment)
    {
      SmtpClient mailClient = new SmtpClient("localhost", 25);

      MailMessage mail = new MailMessage("jarvis@jarvis.cs.usu.edu", to);
      mail.Subject = subject;
      mail.Body = body;
      mail.Attachments.Add(new Attachment(attachment));

      mailClient.Send(mail);

      mailClient.Dispose();
    }

    private string SendFilesToSectionLeaders(string hwPath, string currentCourse, string currentHomework)
    {
      // zip contents
      // email to section leader
      Logger.Info("Sending files to section leaders");
      string[] directories = Directory.GetDirectories(hwPath, "section*", SearchOption.AllDirectories);
      StringBuilder gradingReport = new StringBuilder();
      gradingReport.AppendLine("<p>");
      foreach (string section in directories)
      {
        Logger.Trace("Processing section at {0}", section);
        string sectionNumber = section.Substring(section.LastIndexOf("section"));
        string zipFile = string.Format("{0}/../{1}.zip", section, sectionNumber);

        Logger.Trace("Creating {0} zip file at {1}", sectionNumber, zipFile);
        // zip contents
        if (File.Exists(zipFile))
        {
          File.Delete(zipFile);
        }

        ZipFile.CreateFromDirectory(section, zipFile);

        if (File.Exists(section + "/leader.txt"))
        {
          string leader = File.ReadAllText(section + "/leader.txt");

          Logger.Trace("Emailing zip file to {0}", leader);

          // attach to email to section leader
          SendEmail(leader, 
            "Grades for " + currentCourse + " " + currentHomework,
            "Hello! Attached are the grades for " + currentCourse + " " + currentHomework + ". Happy grading!",
            zipFile);        
        
          gradingReport.AppendLine(string.Format("Emailed section {0} grading materials to {1} <br />", sectionNumber, leader));
        }
        else
        {
          gradingReport.AppendLine(string.Format("Couldn't find section leader for section {0}<br/>", sectionNumber));
        }
      }

      gradingReport.AppendLine("</p>");

      return gradingReport.ToString();
    }
  }
}

