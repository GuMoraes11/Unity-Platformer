using System;
using TarodevController;

[Serializable]
public class PlayerSetupData
{
    public int playerIndex;
    public string playerName;
    public PlayerInput.ControlScheme controlScheme;
    public int skinIndex;
    public bool isReady;
}