﻿// Decompiled with JetBrains decompiler
// Type: SynapseGaming.DRM.ApertureClient.Production.VerifyAuthorizationCodeCompletedEventArgs
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.ComponentModel;

namespace SynapseGaming.DRM.ApertureClient.Production
{
  /// <summary />
  public class VerifyAuthorizationCodeCompletedEventArgs : AsyncCompletedEventArgs
  {
    private object[] object_0;

    /// <remarks />
    public bool Result
    {
      get
      {
        this.RaiseExceptionIfNecessary();
        return (bool) this.object_0[0];
      }
    }

    internal VerifyAuthorizationCodeCompletedEventArgs(object[] object_1, Exception exception_0, bool bool_0, object object_2)
      : base(exception_0, bool_0, object_2)
    {
      this.object_0 = object_1;
    }
  }
}
