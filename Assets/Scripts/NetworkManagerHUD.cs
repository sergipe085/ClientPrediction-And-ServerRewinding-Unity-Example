using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerHUD : MonoBehaviour
{
    [SerializeField] private Button hostButton = null;
    [SerializeField] private Button joinButton = null;

    private void Start() {
        hostButton.onClick.AddListener(() => NetworkManager.Singleton.StartHost());
        joinButton.onClick.AddListener(() => NetworkManager.Singleton.StartClient());

        NetworkManager.Singleton.OnClientConnectedCallback += (ulong clientId) => gameObject.SetActive(false);
    }
}
