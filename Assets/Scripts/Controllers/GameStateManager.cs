using System;
using Data;
using UnityEngine;

namespace Controllers
{
    public class GameStateManager : MonoBehaviour
    {
        public GameState CurrentState { get; private set; } = GameState.Playing;

        public event Action OnGameWon;
        public event Action OnGameLost;

        public void BindToSystems(RackSystem rackSystem, BoardManager boardManager)
        {
            rackSystem.OnRackFull += HandleRackFull;
            boardManager.OnBoardCleared += HandleBoardCleared;
        }

        private void HandleRackFull()
        {
            if(CurrentState != GameState.Playing)
                return;

            SetState(GameState.Lost);
        }

        private void HandleBoardCleared()
        {
            if(CurrentState != GameState.Playing)
                return;
            
            SetState(GameState.Won);
        }

        private void SetState(GameState newState)
        {
            CurrentState = newState;

            switch (newState)
            {
                case GameState.Won:
                    Debug.Log("Won");
                    OnGameWon?.Invoke();
                    break;
                case GameState.Lost:
                    Debug.Log("Lost");
                    OnGameLost?.Invoke();
                    break;
            }
        }
    }
}
