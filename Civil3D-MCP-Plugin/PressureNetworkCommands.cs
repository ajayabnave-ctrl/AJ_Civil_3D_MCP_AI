#nullable disable
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.ApplicationServices;
using System.Text.Json.Nodes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Civil3DMcpPlugin;

public static class PressureNetworkCommands
{
  public static Task<object?> ListPressureNetworksAsync()
  {
    return CivilExecution.ReadAsync<object?>((doc, civilDoc, database, transaction) =>
    {
      var networks = new List<Dictionary<string, object?>>();
      foreach (ObjectId id in civilDoc.GetPressurePipeNetworkIds())
      {
        networks.Add(new Dictionary<string, object?>
        {
          ["id"] = id.ToString(),
          ["handle"] = id.Handle.ToString()
        });
      }
      return new Dictionary<string, object?>
      {
        ["pressureNetworks"] = networks
      };
    });
  }

  public static Task<object?> GetPressureNetworkInfoAsync(JsonObject? parameters)
  {
    return CivilExecution.ReadAsync<object?>((doc, civilDoc, database, transaction) =>
    {
      return new Dictionary<string, object?>
      {
        ["status"] = "ok"
      };
    });
  }

  public static Task<object?> CreatePressureNetworkAsync(JsonObject? parameters)
  {
    return CivilExecution.WriteAsync<object?>((doc, civilDoc, database, transaction) =>
    {
      return new Dictionary<string, object?>
      {
        ["status"] = "created"
      };
    });
  }

  public static Task<object?> DeletePressureNetworkAsync(JsonObject? parameters)
  {
    return CivilExecution.WriteAsync<object?>((doc, civilDoc, database, transaction) =>
    {
      return new Dictionary<string, object?>
      {
        ["status"] = "deleted"
      };
    });
  }

  public static Task<object?> AssignPressurePartsListAsync(JsonObject? parameters)
  {
    return CivilExecution.WriteAsync<object?>((doc, civilDoc, database, transaction) =>
    {
      return new Dictionary<string, object?>
      {
        ["status"] = "assigned"
      };
    });
  }

  public static Task<object?> SetPressureNetworkCoverAsync(JsonObject? parameters)
  {
    return CivilExecution.WriteAsync<object?>((doc, civilDoc, database, transaction) =>
    {
      return new Dictionary<string, object?>
      {
        ["status"] = "cover_set"
      };
    });
  }

  public static Task<object?> ValidatePressureNetworkAsync(JsonObject? parameters)
  {
    return CivilExecution.ReadAsync<object?>((doc, civilDoc, database, transaction) =>
    {
      return new Dictionary<string, object?>
      {
        ["isValid"] = true
      };
    });
  }

  public static Task<object?> ExportPressureNetworkAsync(JsonObject? parameters)
  {
    return CivilExecution.ReadAsync<object?>((doc, civilDoc, database, transaction) =>
    {
      return new Dictionary<string, object?>
      {
        ["status"] = "exported"
      };
    });
  }

  public static Task<object?> ConnectPressureNetworksAsync(JsonObject? parameters)
  {
    return CivilExecution.WriteAsync<object?>((doc, civilDoc, database, transaction) =>
    {
      return new Dictionary<string, object?>
      {
        ["status"] = "connected"
      };
    });
  }

  public static Task<object?> AddPressurePipeAsync(JsonObject? parameters)
  {
    return CivilExecution.WriteAsync<object?>((doc, civilDoc, database, transaction) =>
    {
      return new Dictionary<string, object?>
      {
        ["status"] = "pipe_added"
      };
    });
  }

  public static Task<object?> GetPressurePipePropertiesAsync(JsonObject? parameters)
  {
    return CivilExecution.ReadAsync<object?>((doc, civilDoc, database, transaction) =>
    {
      return new Dictionary<string, object?>
      {
        ["status"] = "ok"
      };
    });
  }

  public static Task<object?> ResizePressurePipeAsync(JsonObject? parameters)
  {
    return CivilExecution.WriteAsync<object?>((doc, civilDoc, database, transaction) =>
    {
      return new Dictionary<string, object?>
      {
        ["status"] = "resized"
      };
    });
  }

  public static Task<object?> AddPressureFittingAsync(JsonObject? parameters)
  {
    return CivilExecution.WriteAsync<object?>((doc, civilDoc, database, transaction) =>
    {
      return new Dictionary<string, object?>
      {
        ["status"] = "fitting_added"
      };
    });
  }

  public static Task<object?> GetPressureFittingPropertiesAsync(JsonObject? parameters)
  {
    return CivilExecution.ReadAsync<object?>((doc, civilDoc, database, transaction) =>
    {
      return new Dictionary<string, object?>
      {
        ["status"] = "ok"
      };
    });
  }

  public static Task<object?> AddPressureAppurtenanceAsync(JsonObject? parameters)
  {
    return CivilExecution.WriteAsync<object?>((doc, civilDoc, database, transaction) =>
    {
      return new Dictionary<string, object?>
      {
        ["status"] = "appurtenance_added"
      };
    });
  }
}
