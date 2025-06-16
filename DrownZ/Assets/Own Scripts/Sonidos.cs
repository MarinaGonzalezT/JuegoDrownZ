using cowsins;
using UnityEngine;

public class Sonidos : MonoBehaviour
{
    private AudioClip sonidoMordida;
    private AudioClip sonidoMuerte;
    public AudioClip sonidoMuerteGhoul;
    public AudioClip sonidoMuerteCreep;
    private AudioClip sonidoHerido;

    private AudioClip sonidoCuracion;

    public AudioClip[] sonidoGhoul;

    public AudioClip[] sonidoCreep;

    public SoundManager soundManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sonidoHerido = Resources.Load<AudioClip>("Impact_Flesh_Gory_Light_001");
        sonidoMordida = Resources.Load<AudioClip>("Impact_Flesh_Gory_Light_002");
        sonidoMuerte = Resources.Load<AudioClip>("Foley_BodyFall_003");
        sonidoCuracion = Resources.Load<AudioClip>("HealthUp Sound");
    }
    public void ReproducirSonidoMordida()
    {
        soundManager.PlaySound(sonidoMordida, 0, 0, false, 0);
    }

    public void ReproducirSonidoMuerte()
    {
        soundManager.PlaySound(sonidoMuerte, 0, 0, false, 0);
    }

    public void ReproducirSonidoMuerteGhoul()
    {
        soundManager.PlaySound(sonidoMuerteGhoul, 0, 0, false, 0);
    }

    public void ReproducirSonidoMuerteCreep()
    {
        soundManager.PlaySound(sonidoMuerteCreep, 0, 0, false, 0);
    }

    public void ReproducirSonidoHerido()
    {
        soundManager.PlaySound(sonidoHerido, 0, 0, false, 0);
    }

    public void ReproducirSonidoGhoul()
    {
        AudioClip grunyidosGhoul = sonidoGhoul[Random.Range(0, sonidoGhoul.Length)];
        soundManager.PlaySound(grunyidosGhoul, 0, 0, false, 1);
    }

    public void ReproducirSonidoCreep()
    {
        AudioClip grunyidosCreep = sonidoCreep[Random.Range(0, sonidoCreep.Length)];
        soundManager.PlaySound(grunyidosCreep, 0, 0, false, 1);
    }

    public void ReproducirSonidoCuracion()
    {
        soundManager.PlaySound(sonidoCuracion, 0, 0, false, 0);
    }
}
