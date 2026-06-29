using UnityEngine;

namespace PinionCore.NetSync
{
    /// <summary>
    /// 提供 <see cref="Server"/> / <see cref="Client"/> 所需的 <see cref="PinionCore.Remote.IProtocol"/>。
    /// Package 僅定義此抽象；具體協議（含實驗性介面）由應用端 (Develop) 以子類別實作，
    /// 並以 ScriptableObject 資產指派到 Server/Client 的 Provider 欄位。
    /// </summary>
    public abstract class ProtocolProvider : ScriptableObject
    {
        public abstract PinionCore.Remote.IProtocol Create();
    }
}
