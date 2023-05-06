using FishNet.Object;

public class GamePlayManager : NetworkBehaviour {
    /// <summary>
    /// Singleton reference for this manager.
    /// </summary>
    public static GamePlayManager Instance;

    private void Awake() {
        FirstInitialize();
    }

    /// <summary>
    /// Initializes this script for use. Should only be completed once.
    /// </summary>
    private void FirstInitialize() {
        //If singleton was somehow loaded twice.
        if(Instance != null && Instance != this) {
            Destroy(this);
            return;
        }

        Instance = this;
    }
}

