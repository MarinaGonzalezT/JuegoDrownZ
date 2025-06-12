using cowsins;
using UnityEngine;

public class Sonidos : MonoBehaviour
{
    private AudioClip sonidoMordida;
    private AudioClip sonidoMuerte;
    private AudioClip sonidoHerido;

    public AudioClip[] sonidoGhoul;

    public SoundManager soundManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sonidoHerido = Resources.Load<AudioClip>("Impact_Flesh_Gory_Light_001");
        sonidoMordida = Resources.Load<AudioClip>("Impact_Flesh_Gory_Light_002");
        sonidoMuerte = Resources.Load<AudioClip>("Foley_BodyFall_003");
        sonidoGhoul = GetComponent<AudioClip[]>();
    }
    public void ReproducirSonidoMordida()
    {
        soundManager.PlaySound(sonidoMordida, 0, 0, false, 1);
    }

    public void ReproducirSonidoMuerte()
    {
        soundManager.PlaySound(sonidoMuerte, 0, 0, false, 1);
    }

    public void ReproducirSonidoHerido()
    {
        soundManager.PlaySound(sonidoHerido, 0, 0, false, 1);
    }

    public void ReproducirSonidoGhoul()
    {
        AudioClip grunyidos = sonidoGhoul[Random.Range(0, sonidoGhoul.Length)];
        soundManager.PlaySound(grunyidos, 0, 0, false, 1);
    }
}
