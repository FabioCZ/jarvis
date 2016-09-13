using System.Diagnostics;
using System.Reflection;
using Nancy.ModelBinding;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jarvis
{
  using Nancy;
  using System;
  using System.Collections.Generic;


  public class JarvisModules : NancyModule
  {
    private FileUploadHandler uploadHandler = new FileUploadHandler();

    public JarvisModules()
    {
      #region Gets
      Get["/"] = _ =>
      {
        Logger.Trace("Handling get for /");
        return View["index"];
      };

      Get["/help"] = _ =>
      {
        Logger.Trace("Handling get for /help");
        return View["help"];
      };

      Get["/grade"] = _ =>
      {
        Logger.Trace("Handling get for /grade");
        return View["grade"];
      };

      Get["/stats"] = _ =>
      {
        Logger.Trace("Handling get for /stats");
        return View["stats", Jarvis.Stats];
      };
      #endregion

      #region NewGets

      Get["/version"] = _ =>
      {
        Logger.Trace("Handling get for /version");
        var assembly = Assembly.GetExecutingAssembly();
        var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
        var version = fvi.FileVersion;
        var json = new JObject();
        json["version"] = version;
        return json.ToString();
      };
      #endregion


      #region newPosts
      Post["/run"] = _ =>
      {
        Logger.Trace("Handling post for /run");
        Grader grader = new Grader();
        string toReturn;

        var request = this.Bind<FileUploadRequest>();
        var assignment = uploadHandler.HandleStudentUpload(request.File);

        Logger.Info("Received assignment from {0} for {1} HW#{2} with {3} header", assignment.StudentId, assignment.Course, assignment.HomeworkId, assignment.ValidHeader ? "true" : "false");

        if (assignment.ValidHeader)
        {
          // Run grader
          Logger.Debug("Assignment header was valid");
          var result = grader.Grade(assignment);
          toReturn = JsonConvert.SerializeObject(result);
        }
        else
        {
          var json = new JObject();
          json["headerError"] = assignment.ErrorMessage;
          toReturn = json.ToString();
        }

        Jarvis.Stats.WriteStats();
        return toReturn;
      };
      #endregion

      #region Posts
//      Post["/run"] = _ =>
//{
//  Logger.Trace("Handling post for /practiceRun");
//  Grader grader = new Grader();
//  StringBuilder builder = new StringBuilder();

//  var request = this.Bind<FileUploadRequest>();
//  var assignment = uploadHandler.HandleStudentUpload(request.File);

//  Logger.Info("Received assignment from {0} for {1} HW#{2} with {3} header", assignment.StudentId, assignment.Course, assignment.HomeworkId, assignment.ValidHeader ? "true" : "false");

//  if (assignment.ValidHeader)
//  {
//    // Run grader
//    Logger.Debug("Assignment header was valid");
//    GradingResult result = null;
//    result = grader.Grade(assignment);
//    builder.Append(result.ToHtml());
//  }
//  else
//  {
//    builder.AppendLine("<p>");
//    builder.AppendLine("The uploaded file contains an invalid header, sir. I suggest you review the <a href='/help'>help</a>.");
//    builder.AppendFormat("<br />Parser error message: {0}", assignment.ErrorMessage);
//    builder.AppendLine("</p>");
//  }

//  Jarvis.Stats.WriteStats();

//  return builder.ToString();
//};

      Post["/grade"] = _ =>
      {
        Logger.Trace("Handling post for /runForRecord");
        Guid temp = Guid.NewGuid();
        string baseDir = Jarvis.Config.AppSettings.Settings["workingDir"].Value;
        string gradingDir = baseDir + "/grading/" + temp.ToString() + "/";

        var request = this.Bind<FileUploadRequest>();
        List<Assignment> assignments = uploadHandler.HandleGraderUpload(gradingDir, request.File);

        Grader grader = new Grader();

        return grader.GenerateGrades(baseDir, assignments);
      };
      #endregion

      // MOSS - To be written to a file
    }
  }
}