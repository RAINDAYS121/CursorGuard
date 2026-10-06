// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 RAINDAYS121.
using System;

namespace LoLMouseGuard
{
    // The worker owns this state. Editing suspends protection without turning
    // that temporary suspension into a permanent change of user intent.
    sealed class SettingsSession
    {
        readonly GuardCore core;
        readonly SettingsService settings;
        bool editing, resume, startupHandled, wantedIntent;
        public SettingsSession(GuardCore guard, SettingsService service) {core=guard;settings=service;wantedIntent=settings.Current.ProtectionEnabled;}
        public bool Editing {get {return editing;}}
        public bool EnabledIntent {get {return editing?resume:core.Enabled;}}
        // Restore the saved intent once. Hotkeys/DPI/cleanup gates still apply,
        // and Enable only arms the usual foreground/stability/cursor checks.
        public void RestoreOnStartup(out string notice)
        {
            notice=null;if(startupHandled) return;startupHandled=true;
            if(!settings.Current.ProtectionEnabled) return;
            if(core.CanEnable && !core.OwnsClip) core.Enable();
            else Disarm(out notice);
        }
        public bool Enable(out string notice)
        {
            notice=null;startupHandled=true;
            if(editing || !core.CanEnable || core.OwnsClip) return false;
            if(!settings.SaveProtectionIntent(true,out notice)) {wantedIntent=false;core.Pause(false);return false;}
            wantedIntent=true;
            return core.Enable();
        }
        static string Combine(string first,string second)
        {return second==null?first:(first==null?second:first+" "+second);}
        void Disarm(out string notice) {wantedIntent=false;settings.SaveProtectionIntent(false,out notice);}
        public void ObserveFault(out string notice)
        {
            notice=null;
            if(!editing && !core.Enabled && wantedIntent) Disarm(out notice);
        }
        public bool Begin(out string notice)
        {
            notice=null;startupHandled=true;
            if(editing) return true; // Repeated entry must not capture the suspended state.
            bool previous=core.Enabled;
            if(!SettingsEntry.TryBegin(core,settings,ref editing))
            {resume=false;string failure;Disarm(out failure);notice=Combine("尚未确认释放本工具约束，不能编辑。请使用紧急释放或正常 Alt+Tab。",failure);return false;}
            resume=previous;
            core.SetHotkeys(false,"编辑设置期间保持暂停。Alt+F4 可随时退出。");
            return true;
        }
        public void Pause(bool emergency) {string notice;Pause(emergency,out notice);}
        public void Pause(bool emergency,out string notice)
        {startupHandled=true;resume=false;core.Pause(emergency);Disarm(out notice);}
        public void Abort() {string notice;Abort(out notice);}
        public void Abort(out string notice)
        {startupHandled=true;resume=false;core.Pause(false);Disarm(out notice);}
        // Normal exit releases the current constraint but keeps the saved choice.
        public void Shutdown() {string notice;Shutdown(out notice);}
        public void Shutdown(out string notice)
        {
            startupHandled=true;resume=false;core.Pause(false);notice=null;
            if(core.OwnsClip) Disarm(out notice); // A failed exit cleanup is a protection fault.
        }
        string Finish(bool restore, string keyError)
        {
            bool wanted=editing && resume && restore;
            editing=false;resume=false;
            core.SetHotkeys(settings.Ready,keyError);
            if(wanted && core.CanEnable && !core.OwnsClip) core.Enable();
            string failure=null;
            if(!restore || (wanted && !core.Enabled)) Disarm(out failure);
            return failure;
        }
        public bool Cancel(out string notice)
        {
            notice=null;if(!editing) return true;
            bool ready=settings.RegisterCurrent(out notice);
            notice=Combine(notice,Finish(ready,notice));return ready;
        }
        public bool Save(Settings candidate,out string notice)
        {
            if(!editing) {notice="请先打开设置，保护暂停后才能修改。";return false;}
            candidate=candidate.Copy();candidate.ProtectionEnabled=wantedIntent;
            bool saved=settings.Save(candidate,out notice);
            string keyError=null;
            // Validation failure can occur before Save restores shortcuts.
            if(!settings.Ready) settings.RegisterCurrent(out keyError);
            notice=Combine(notice,Finish(saved,keyError??notice));
            return saved;
        }
    }
}
