﻿namespace OpenRasta.Plugins.Hydra.Schemas
{

  public static partial class HydraCore
  {
    public class Operation
    {
      public string Method { get; set; }
      public string Expects { get; set; }
      public string Title { get; set; }
    }
  }
}