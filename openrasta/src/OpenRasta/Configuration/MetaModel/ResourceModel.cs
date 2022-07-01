﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OpenRasta.TypeSystem;

namespace OpenRasta.Configuration.MetaModel
{
  public class ResourceModel : ConfigurationModel
  {
    object _resourceKey;

    public ResourceModel()
    {
      Uris = new List<UriModel>();
      Handlers = new List<HandlerModel>();
      Codecs = new List<CodecModel>();
    }

    public IList<CodecModel> Codecs { get; }
    public IList<HandlerModel> Handlers { get; }

    public bool IsStrictRegistration { get; set; }

    public object ResourceKey
    {
      get => _resourceKey;
      set
      {
        _resourceKey = value;
        if (ResourceType == null)
          ResourceType = (value as IType)?.StaticType ?? value as Type;
      }
    }

    public Type ResourceType { get; set; }

    public IList<UriModel> Uris { get; }
    
    public ClassDefinition ClassDefinition { get; set; }
    public List<ResourceLinkModel> Links { get;  } = new List<ResourceLinkModel>();
    public string Name { get; set; }

    public override string ToString()
    {
      return
        $"Key: {ResourceKey}, Uris: {Uris.Aggregate(string.Empty, (str, reg) => str + reg + ";")}, Handlers: {Handlers.Aggregate(string.Empty, (str, reg) => str + reg + ";")}, Codecs: {Codecs.Aggregate(string.Empty, (str, reg) => str + reg + ";")}";
    }
  }
}