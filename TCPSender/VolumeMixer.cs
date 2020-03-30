using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TCPSender
{
    public sealed class VolumeMaster
    {
        public List<AudioSession> Sessions { get; }

        public VolumeMaster()
        {
            // get the speakers (1st render + multimedia) device
            List<AudioSession> list = new List<AudioSession>();
            IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            IMMDevice speakers;
            deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

            Guid GUID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
            object o;
            speakers.Activate(ref GUID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
            IAudioSessionManager2 mgr = (IAudioSessionManager2)o;

            if (mgr == null)
            {
                Sessions = list;
                return;
            }
               
            IAudioSessionEnumerator sessionEnumerator;
            mgr.GetSessionEnumerator(out sessionEnumerator);

            int count;
            sessionEnumerator.GetCount(out count);

            for (int i = 0; i < count; i++)
            {
                IAudioSessionControl ctl;
                sessionEnumerator.GetSession(i, out ctl);
                if (ctl == null)
                    continue;

                IAudioSessionControl2 ctl2 = ctl as IAudioSessionControl2;
                if (ctl2 != null)
                {
                    list.Add(new AudioSession(ctl2));
                }
            }
            Marshal.ReleaseComObject(speakers);
            Marshal.ReleaseComObject(deviceEnumerator);
            Marshal.ReleaseComObject(sessionEnumerator);
            Marshal.ReleaseComObject(mgr);

            Sessions = list;
        }

        public AudioSession GetSessionByDisplayName(string name)
        {
            foreach(AudioSession session in Sessions)
            {
                if(name == session.DisplayName)
                {
                    return session;
                }
            }
            return null;
        }

        public AudioSession GetSessionByProcessName(string name)
        {
            foreach (AudioSession session in Sessions)
            {
                if (session.Process != null)
                {
                    if(name == session.Process.ProcessName)
                    {
                        return session;
                    }
                }
            }
            return null;
        }

        public AudioSession GetSessionByProcessID(int id)
        {
            foreach (AudioSession session in Sessions)
            {
                if (session.Process != null)
                {
                    if (id == session.Process.Id)
                    {
                        return session;
                    }
                }
            }
            return null;
        }
    }

    public sealed class AudioSession : IDisposable
    {
        private IAudioSessionControl2 _ctl;
        private Process _process;
        private int ID;

        internal AudioSession(IAudioSessionControl2 ctl)
        {
            _ctl = ctl;
        }

        public float? Volume
        {
            get
            {
                ISimpleAudioVolume volume = _ctl as ISimpleAudioVolume;
                if (volume == null)
                    return null;

                float level;
                volume.GetMasterVolume(out level);
                return level * 100;
            }
            set
            {
                ISimpleAudioVolume volume = _ctl as ISimpleAudioVolume;
                if (volume == null)
                    return;

                float newLevel = value.HasValue ? value.Value : 0;
                Guid guid = Guid.Empty;
                volume.SetMasterVolume(newLevel / 100, ref guid);

            }
        }

        public bool? Mute
        {
            get
            {
                ISimpleAudioVolume volume = _ctl as ISimpleAudioVolume;
                if (volume == null)
                    return null;

                bool mute;
                volume.GetMute(out mute);
                return mute;
            }
            set
            {
                if (value != null)
                {
                    ISimpleAudioVolume volume = _ctl as ISimpleAudioVolume;
                    if (volume == null)
                        return;

                    Guid guid = Guid.Empty;
                    bool newBool = value.HasValue ? value.Value : false;
                    volume.SetMute(newBool, ref guid);
                }
            }
        }

        public Process Process
        {
            get
            {
                if (_process == null && ProcessId != 0)
                {
                    try
                    {
                        _process = Process.GetProcessById(ProcessId);
                    }
                    catch
                    {
                        // do nothing
                    }
                }
                return _process;
            }
        }

        public int ProcessId
        {
            get
            {
                CheckDisposed();
                int i;
                _ctl.GetProcessId(out i);
                return i;
            }
        }

        public string Identifier
        {
            get
            {
                CheckDisposed();
                string s;
                _ctl.GetSessionIdentifier(out s);
                return s;
            }
        }

        public string InstanceIdentifier
        {
            get
            {
                CheckDisposed();
                string s;
                _ctl.GetSessionInstanceIdentifier(out s);
                return s;
            }
        }

        public AudioSessionState State
        {
            get
            {
                CheckDisposed();
                AudioSessionState s;
                _ctl.GetState(out s);
                return s;
            }
        }

        public Guid GroupingParam
        {
            get
            {
                CheckDisposed();
                Guid g;
                _ctl.GetGroupingParam(out g);
                return g;
            }
            set
            {
                CheckDisposed();
                _ctl.SetGroupingParam(value, Guid.Empty);
            }
        }

        public string DisplayName
        {
            get
            {
                CheckDisposed();
                string s;
                _ctl.GetDisplayName(out s);
                return s;
            }
            set
            {
                CheckDisposed();
                string s;
                _ctl.GetDisplayName(out s);
                if (s != value)
                {
                    _ctl.SetDisplayName(value, Guid.Empty);
                }
            }
        }

        public string IconPath
        {
            get
            {
                CheckDisposed();
                string s;
                _ctl.GetIconPath(out s);
                return s;
            }
            set
            {
                CheckDisposed();
                string s;
                _ctl.GetIconPath(out s);
                if (s != value)
                {
                    _ctl.SetIconPath(value, Guid.Empty);
                }
            }
        }

        private void CheckDisposed()
        {
            if (_ctl == null)
                throw new ObjectDisposedException("Control");
        }

        public override string ToString()
        {
            string s = DisplayName;
            if (!string.IsNullOrEmpty(s))
                return "DisplayName: " + s;

            if (Process != null)
                return "Process: " + Process.ProcessName;

            return "Pid: " + ProcessId;
        }

        public void Dispose()
        {
            if (_ctl != null)
            {
                Marshal.ReleaseComObject(_ctl);
                _ctl = null;
            }
        }



        public static List<AudioSession> GetAllSessions2()
        {
            // get the speakers (1st render + multimedia) device
            List<AudioSession> list = new List<AudioSession>();
            IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            IMMDevice speakers;
            deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

            Guid GUID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
            object o;
            speakers.Activate(ref GUID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
            IAudioSessionManager2 mgr = (IAudioSessionManager2)o;

            if (mgr == null)
                return list;

            IAudioSessionEnumerator sessionEnumerator;
            mgr.GetSessionEnumerator(out sessionEnumerator);

            int count;
            sessionEnumerator.GetCount(out count);

            for (int i = 0; i < count; i++)
            {
                IAudioSessionControl ctl;
                sessionEnumerator.GetSession(i, out ctl);
                if (ctl == null)
                    continue;

                IAudioSessionControl2 ctl2 = ctl as IAudioSessionControl2;
                if (ctl2 != null)
                {
                    list.Add(new AudioSession(ctl2));
                }
            }
            Marshal.ReleaseComObject(speakers);
            Marshal.ReleaseComObject(deviceEnumerator);
            Marshal.ReleaseComObject(sessionEnumerator);
            Marshal.ReleaseComObject(mgr);
            return list;
        }
    }


    public enum AudioSessionState
    {
        Inactive = 0,
        Active = 1,
        Expired = 2
    }

    public enum AudioDeviceState
    {
        Active = 0x1,
        Disabled = 0x2,
        NotPresent = 0x4,
        Unplugged = 0x8,
    }

    public enum AudioSessionDisconnectReason
    {
        DisconnectReasonDeviceRemoval = 0,
        DisconnectReasonServerShutdown = 1,
        DisconnectReasonFormatChanged = 2,
        DisconnectReasonSessionLogoff = 3,
        DisconnectReasonSessionDisconnected = 4,
        DisconnectReasonExclusiveModeOverride = 5
    }



    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumerator
    {
    }

    [Flags]
    internal enum EDataFlow
    {
        eRender,
        eCapture,
        eAll,
    }

    internal enum ERole
    {
        eConsole,
        eMultimedia,
        eCommunications,
    }

    internal enum DEVICE_STATE
    {
        ACTIVE = 0x00000001,
        DISABLED = 0x00000002,
        NOTPRESENT = 0x00000004,
        UNPLUGGED = 0x00000008,
        MASK_ALL = 0x0000000F
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PROPERTYKEY
    {
        public Guid fmtid;
        public int pid;

        public override string ToString()
        {
            return fmtid.ToString("B") + " " + pid;
        }
    }



    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceEnumerator
    {
        [PreserveSig]
        int EnumAudioEndpoints(EDataFlow dataFlow, DEVICE_STATE dwStateMask, out IMMDeviceCollection ppDevices);

        [PreserveSig]
        int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);

        [PreserveSig]
        int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);

        [PreserveSig]
        int RegisterEndpointNotificationCallback(IMMNotificationClient pClient);

        [PreserveSig]
        int UnregisterEndpointNotificationCallback(IMMNotificationClient pClient);
    }

    [Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMNotificationClient
    {
        void OnDeviceStateChanged([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId, DEVICE_STATE dwNewState);
        void OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId);
        void OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string deviceId);
        void OnDefaultDeviceChanged(EDataFlow flow, ERole role, string pwstrDefaultDeviceId);
        void OnPropertyValueChanged([MarshalAs(UnmanagedType.LPWStr)] string pwstrDeviceId, PROPERTYKEY key);
    }

    [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceCollection
    {
        [PreserveSig]
        int GetCount(out int pcDevices);

        [PreserveSig]
        int Item(int nDevice, out IMMDevice ppDevice);
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

        [PreserveSig]
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);

        [PreserveSig]
        int GetState(out DEVICE_STATE pdwState);
    }

    [Guid("6f79d558-3e96-4549-a1d1-7d75d2288814"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPropertyDescription
    {
        [PreserveSig]
        int GetPropertyKey(out PROPERTYKEY pkey);

        [PreserveSig]
        int GetCanonicalName(out IntPtr ppszName);

        [PreserveSig]
        int GetPropertyType(out short pvartype);

        [PreserveSig]
        int GetDisplayName(out IntPtr ppszName);

        // WARNING: the rest is undefined. you *can't* implement it, only use it.
    }


    [Guid("BFA971F1-4D5E-40BB-935E-967039BFBEE4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionManager
    {
        [PreserveSig]
        int GetAudioSessionControl([MarshalAs(UnmanagedType.LPStruct)] Guid AudioSessionGuid, int StreamFlags, out IAudioSessionControl SessionControl);

        [PreserveSig]
        int GetSimpleAudioVolume([MarshalAs(UnmanagedType.LPStruct)] Guid AudioSessionGuid, int StreamFlags, ISimpleAudioVolume AudioVolume);
    }

    [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionManager2
    {
        [PreserveSig]
        int GetAudioSessionControl([MarshalAs(UnmanagedType.LPStruct)] Guid AudioSessionGuid, int StreamFlags, out IAudioSessionControl SessionControl);

        [PreserveSig]
        int GetSimpleAudioVolume([MarshalAs(UnmanagedType.LPStruct)] Guid AudioSessionGuid, int StreamFlags, ISimpleAudioVolume AudioVolume);

        [PreserveSig]
        int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);

        [PreserveSig]
        int RegisterSessionNotification(IAudioSessionNotification SessionNotification);

        [PreserveSig]
        int UnregisterSessionNotification(IAudioSessionNotification SessionNotification);

        int RegisterDuckNotificationNotImpl();
        int UnregisterDuckNotificationNotImpl();
    }

    [Guid("641DD20B-4D41-49CC-ABA3-174B9477BB08"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionNotification
    {
        void OnSessionCreated(IAudioSessionControl NewSession);
    }

    [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionEnumerator
    {
        [PreserveSig]
        int GetCount(out int SessionCount);

        [PreserveSig]
        int GetSession(int SessionCount, out IAudioSessionControl Session);
    }

    [Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionControl2
    {
        // IAudioSessionControl
        [PreserveSig]
        int GetState(out AudioSessionState pRetVal);

        [PreserveSig]
        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)]string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

        [PreserveSig]
        int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

        [PreserveSig]
        int GetGroupingParam(out Guid pRetVal);

        [PreserveSig]
        int SetGroupingParam([MarshalAs(UnmanagedType.LPStruct)] Guid Override, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

        [PreserveSig]
        int RegisterAudioSessionNotification(IAudioSessionEvents NewNotifications);

        [PreserveSig]
        int UnregisterAudioSessionNotification(IAudioSessionEvents NewNotifications);

        // IAudioSessionControl2
        [PreserveSig]
        int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int GetProcessId(out int pRetVal);

        [PreserveSig]
        int IsSystemSoundsSession();

        [PreserveSig]
        int SetDuckingPreference(bool optOut);
    }

    [Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionControl
    {
        [PreserveSig]
        int GetState(out AudioSessionState pRetVal);

        [PreserveSig]
        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)]string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

        [PreserveSig]
        int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

        [PreserveSig]
        int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

        [PreserveSig]
        int GetGroupingParam(out Guid pRetVal);

        [PreserveSig]
        int SetGroupingParam([MarshalAs(UnmanagedType.LPStruct)] Guid Override, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

        [PreserveSig]
        int RegisterAudioSessionNotification(IAudioSessionEvents NewNotifications);

        [PreserveSig]
        int UnregisterAudioSessionNotification(IAudioSessionEvents NewNotifications);
    }

    [Guid("24918ACC-64B3-37C1-8CA9-74A66E9957A8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioSessionEvents
    {
        void OnDisplayNameChanged([MarshalAs(UnmanagedType.LPWStr)] string NewDisplayName, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
        void OnIconPathChanged([MarshalAs(UnmanagedType.LPWStr)] string NewIconPath, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
        void OnSimpleVolumeChanged(float NewVolume, bool NewMute, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
        void OnChannelVolumeChanged(int ChannelCount, IntPtr NewChannelVolumeArray, int ChangedChannel, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
        void OnGroupingParamChanged([MarshalAs(UnmanagedType.LPStruct)] Guid NewGroupingParam, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
        void OnStateChanged(AudioSessionState NewState);
        void OnSessionDisconnected(AudioSessionDisconnectReason DisconnectReason);
    }

    [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISimpleAudioVolume
    {
        [PreserveSig]
        int SetMasterVolume(float fLevel, ref Guid EventContext);

        [PreserveSig]
        int GetMasterVolume(out float pfLevel);

        [PreserveSig]
        int SetMute(bool bMute, ref Guid EventContext);

        [PreserveSig]
        int GetMute(out bool pbMute);
    }
}
