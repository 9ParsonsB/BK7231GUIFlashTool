﻿namespace BK7231Flasher
{
    public interface ILogListener
    {
        void addLog(string s, Color c);
        void setProgress(int cur, int max);
        void setState(string s, Color col);
    }
}
