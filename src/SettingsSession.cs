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
        bool editing, resume;
        public SettingsSession(GuardCore guard, SettingsService service) {core=guard;settings=service;}
        public bool Editing {get {return editing;}}
        public bool EnabledIntent {get {return editing?resume:core.Enabled;}}
        public bool Begin(out string notice)
        {
            notice=null;
            if(editing) return true; // Repeated entry must not capture the suspended state.
            bool previous=core.Enabled;
            if(!SettingsEntry.TryBegin(core,settings,ref editing))
            {resume=false;notice="尚未确认释放本工具约束，不能编辑。请使用紧急释放或正常 Alt+Tab。";return false;}
            resume=previous;
            core.SetHotkeys(false,"编辑设置期间保持暂停。Alt+F4 可随时退出。");
            return true;
        }
        public void Pause(bool emergency) {resume=false;core.Pause(emergency);}
        public void Abort() {resume=false;core.Pause(false);}
        void Finish(bool restore, string keyError)
        {
            bool wanted=editing && resume && restore;
            editing=false;resume=false;
            core.SetHotkeys(settings.Ready,keyError);
            if(wanted && core.CanEnable && !core.OwnsClip) core.Enable();
        }
        public bool Cancel(out string notice)
        {
            notice=null;if(!editing) return true;
            bool ready=settings.RegisterCurrent(out notice);
            Finish(ready,notice);return ready;
        }
        public bool Save(Settings candidate,out string notice)
        {
            if(!editing) {notice="请先打开设置，保护暂停后才能修改。";return false;}
            bool saved=settings.Save(candidate,out notice);
            string keyError=null;
            // Validation failure can occur before Save restores shortcuts.
            if(!settings.Ready) settings.RegisterCurrent(out keyError);
            Finish(saved,keyError??notice);
            return saved;
        }
    }
}
