param([Parameter(Mandatory = $true)][int]$TargetPid, [Parameter(Mandatory = $true)][string]$Log)
# 撮影中の Reader の窓を、ノートの画面へ出さないための見張り。
# 製品は起動後に窓を画面の中央へ動かすので、その窓 (と同じプロセスの最上位の窓) を見つけたら
# すぐ画面外 (-32000, -32000) へ戻し、タスクバーのボタンも外す。窓の中身は CDP で撮る。
# 対象のプロセスが終わったら自分も終わる。何をしたかは $Log へ時刻つきで残す。
$ErrorActionPreference = 'Stop'
function Write-Rig([string]$text) {
    [IO.File]::AppendAllText($Log, (Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff') + "`t" + $text + "`r`n", (New-Object Text.UTF8Encoding($false)))
}
Write-Rig ("start watcher pid=" + $PID + " target=" + $TargetPid)
try {
    Add-Type @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
public static class RigOffscreen {
  public delegate bool EnumProc(IntPtr h, IntPtr l);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
  [ComImport, Guid("56FDF342-FD6D-11d0-958A-006097C9A090"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  interface ITaskbarList { void HrInit(); void AddTab(IntPtr h); void DeleteTab(IntPtr h); void ActivateTab(IntPtr h); void SetActiveAlt(IntPtr h); }
  [ComImport, Guid("56FDF344-FD6D-11d0-958A-006097C9A090")] class TaskbarList { }
  public static List<IntPtr> Windows(uint pid) {
    List<IntPtr> found = new List<IntPtr>();
    EnumWindows(delegate(IntPtr h, IntPtr l) { uint p; GetWindowThreadProcessId(h, out p); if (p == pid && IsWindowVisible(h)) { found.Add(h); } return true; }, IntPtr.Zero);
    return found;
  }
  public static void Untab(IntPtr h) {
    ITaskbarList list = (ITaskbarList)new TaskbarList();
    list.HrInit();
    list.DeleteTab(h);
  }
}
"@
} catch { Write-Rig ("add-type failed: " + $_.Exception.Message); throw }
$untabbed = @{}
Write-Rig ("watch target=" + $TargetPid)
while ($true) {
    $proc = Get-Process -Id $TargetPid -ErrorAction SilentlyContinue
    if ($null -eq $proc) { break }
    foreach ($h in [RigOffscreen]::Windows([uint32]$TargetPid)) {
        $r = New-Object RigOffscreen+RECT
        [void][RigOffscreen]::GetWindowRect($h, [ref]$r)
        if ($r.Left -gt -20000 -and $r.Top -gt -20000) {
            # SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE
            [void][RigOffscreen]::SetWindowPos($h, [IntPtr]::Zero, -32000, -32000, 0, 0, 0x0001 -bor 0x0004 -bor 0x0010)
            Write-Rig ("moved hwnd=" + $h + " from=" + $r.Left + "," + $r.Top + " size=" + ($r.Right - $r.Left) + "x" + ($r.Bottom - $r.Top))
        }
        if (-not $untabbed.ContainsKey([string]$h)) {
            try { [RigOffscreen]::Untab($h); $untabbed[[string]$h] = $true; Write-Rig ("untabbed hwnd=" + $h) }
            catch { $untabbed[[string]$h] = $true; Write-Rig ("untab failed hwnd=" + $h + ": " + $_.Exception.Message) }
        }
    }
    Start-Sleep -Milliseconds 10
}
Write-Rig "target exited"
