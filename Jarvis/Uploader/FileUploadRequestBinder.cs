﻿using Nancy;
using Nancy.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis
{
  public class FileUploadRequestBinder : IModelBinder
  {
    public object Bind(NancyContext context, Type modelType, object instance, BindingConfig configuration, params string[] blackList)
    {
      var fileUploadRequest = (instance as FileUploadRequest) ?? new FileUploadRequest();

      var form = context.Request.Form;

      fileUploadRequest.File = GetFileByKey(context, "file");

      return fileUploadRequest;
    }

    private IList<string> GetTags(dynamic field)
    {
      try
      {
        var tags = (string)field;
        return tags.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
      }
      catch
      {
        return new List<string>();
      }
    }

    private HttpFile GetFileByKey(NancyContext context, string key)
    {
      IEnumerable<HttpFile> files = context.Request.Files;
      if (files != null)
      {
        return files.FirstOrDefault(x => x.Key == key);
      }
      return null;
    }

    public bool CanBind(Type modelType)
    {
      return modelType == typeof(FileUploadRequest);
    }
  }
}
