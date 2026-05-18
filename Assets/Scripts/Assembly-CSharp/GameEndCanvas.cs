using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEndCanvas : MonoBehaviour
{
	public void Restart()
	{
		SaveManager.Current.FULL_ClearSave();
		SceneManager.LoadScene(0);
	}
}
