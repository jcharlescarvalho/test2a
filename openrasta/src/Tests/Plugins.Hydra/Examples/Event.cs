﻿using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Tests.Plugins.Hydra
{
  public class Event
  {
    public Event()
    {
      Customers = new List<Customer>();
    }
    public int Id { get; set; }
    
    public string FirstName { get; set; }

    public string AlwaysNullString { get; set; }

    public Customer Customer { get; set; }

    public List<Customer> Customers { get; set; }

    [JsonIgnore]
    [IgnoreDataMember]
    public int Age { get; set; }
  }
}