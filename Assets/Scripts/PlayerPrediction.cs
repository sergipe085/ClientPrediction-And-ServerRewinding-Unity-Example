using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[System.Serializable]
public class PlayerState : INetworkSerializable {
    public int requestId;
    public string playerInput;
    public Vector3 position;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref requestId);
        serializer.SerializeValue(ref playerInput);
        serializer.SerializeValue(ref position);
    }
}

public class PlayerPrediction : NetworkBehaviour
{
    [SerializeField] private int updatesPerSecond = 4;

    private int current = 0;
    private List<PlayerState> playerInputsToSendToServer = new List<PlayerState>();
    private List<PlayerState> unProceccedPlayerInputs = new List<PlayerState>();

    private float time = 0.0f;

    private void Update() {
        if (!IsOwner) return;

        time += Time.deltaTime;

        if (time >= 1f / updatesPerSecond) {
            time = 0.0f;
            SendInputsToServer();
        }

        bool left = Input.GetKey(KeyCode.A);
        bool right = Input.GetKey(KeyCode.D);

        if (right || left && right != left) {
            PlayerState playerState = new PlayerState() {
                requestId = current,
                playerInput = right == true ? "right" : "left",
                position = transform.position
            };

            if (!IsServer) {
                ClientPrediction(playerState);
                current++;
            }
            else {
                transform.position += ProcessPlayerInput(playerState.playerInput);
            }

        }
    }

    private void ClientPrediction(PlayerState playerState) {
        playerInputsToSendToServer.Add(playerState);
        unProceccedPlayerInputs.Add(playerState);
        transform.position += ProcessPlayerInput(playerState.playerInput);
    }

    private void SendInputsToServer() {
        SendInputServerRpc(playerInputsToSendToServer.ToArray());
        playerInputsToSendToServer.Clear();
    }

    [ServerRpc]
    private void SendInputServerRpc(PlayerState[] playerStates) {
        Vector3 positionToAdd = Vector3.zero;
        List<int> procecedIds = new List<int>();
        foreach(PlayerState state in playerStates) {
            positionToAdd += ProcessPlayerInput(state.playerInput);
            procecedIds.Add(state.requestId);
        }
        transform.position += positionToAdd;
        ServerReconciliationClientRpc(transform.position, procecedIds.ToArray());
    }

    [ClientRpc]
    private void ServerReconciliationClientRpc(Vector3 position, int[] inputsIds) {
        foreach(int id in inputsIds) {
            unProceccedPlayerInputs.Remove(unProceccedPlayerInputs.Find(b => b.requestId == id));
        }

        transform.position = position;

        Vector3 positionToAdd = Vector3.zero;

        foreach(PlayerState state in unProceccedPlayerInputs) {
            positionToAdd += ProcessPlayerInput(state.playerInput);
        }

        transform.position += positionToAdd;
    }

    private Vector3 ProcessPlayerInput(string playerInput) {
        switch(playerInput) {
            case "right":
                return Vector3.right * 0.1f;

            case "left":
                return Vector3.right * -0.1f;
            
            default:
                return Vector3.zero;
        }
    }
}
