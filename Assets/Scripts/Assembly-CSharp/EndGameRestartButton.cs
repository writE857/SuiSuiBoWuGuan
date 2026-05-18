using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGameRestartButton : MonoBehaviour
{
	public void ENDGAMERESTART()
	{
		SaveManager.Current.FULL_ClearSave();
		SceneManager.LoadScene(0);
	}
}
