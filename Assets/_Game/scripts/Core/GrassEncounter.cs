using System;
using System.Collections.Generic;
using Pokemon.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[Serializable]
public sealed class GrassEncounterEntry
{
    [SerializeField] private PokemonSpeciesData species;
    [SerializeField, Min(1)] private int minLevel = 3;
    [SerializeField, Min(1)] private int maxLevel = 6;
    [SerializeField, Min(0f)] private float weight = 1f;

    public PokemonSpeciesData Species => species;
    public float Weight => weight;

    public int RollLevel()
    {
        int lower = Mathf.Max(1, Mathf.Min(minLevel, maxLevel));
        int upper = Mathf.Max(lower, Mathf.Max(minLevel, maxLevel));
        return UnityEngine.Random.Range(lower, upper + 1);
    }
}

public class GrassEncounter : MonoBehaviour
{
    private static readonly HashSet<GrassEncounter> ActiveEncounters = new HashSet<GrassEncounter>();

    [Header("Encounter Settings")]
    [SerializeField, Range(0f, 1f)] private float encounterRate = 0.2f;
    [SerializeField, Min(0.01f)] private float distancePerCheck = 1f;

    [Header("Pokemon Pool")]
    [SerializeField] private List<GrassEncounterEntry> encounters = new List<GrassEncounterEntry>();

    private Collider2D[] _colliders;
    private Tilemap _tilemap;

    public float DistancePerCheck => Mathf.Max(0.01f, distancePerCheck);

    private void Awake()
    {
        CacheAreaComponents();
    }

    private void OnEnable()
    {
        CacheAreaComponents();
        ActiveEncounters.Add(this);
    }

    private void OnDisable()
    {
        ActiveEncounters.Remove(this);
    }

    public static GrassEncounter FindAtPosition(Vector2 worldPosition, LayerMask fallbackGrassLayer)
    {
        foreach (GrassEncounter encounter in ActiveEncounters)
        {
            if (encounter != null && encounter.Contains(worldPosition))
            {
                return encounter;
            }
        }

        Collider2D hit = Physics2D.OverlapPoint(worldPosition, fallbackGrassLayer);
        return hit == null ? null : hit.GetComponentInParent<GrassEncounter>();
    }

    public bool TryStartEncounter(Transform player)
    {
        float rate = Mathf.Clamp01(encounterRate);
        if (player == null || rate <= 0f || (rate < 1f && UnityEngine.Random.value >= rate))
        {
            return false;
        }

        SceneTransitionManager transitionManager = SceneTransitionManager.Instance;
        if (transitionManager == null)
        {
            Debug.LogError("Cannot enter battle because SceneTransitionManager is missing.", this);
            return false;
        }

        GrassEncounterEntry selected = PickEncounter();
        PokemonSpeciesData species = selected == null ? null : selected.Species;
        int level = selected == null ? 0 : selected.RollLevel();

        transitionManager.EnterBattle(
            player.position,
            SceneManager.GetActiveScene().name,
            species,
            level);
        return true;
    }

    private void CacheAreaComponents()
    {
        _colliders = GetComponents<Collider2D>();
        _tilemap = GetComponent<Tilemap>();
    }

    private bool Contains(Vector2 worldPosition)
    {
        if (_tilemap != null && _tilemap.HasTile(_tilemap.WorldToCell(worldPosition)))
        {
            return true;
        }

        if (_colliders == null)
        {
            return false;
        }

        foreach (Collider2D areaCollider in _colliders)
        {
            if (areaCollider != null && areaCollider.enabled && areaCollider.OverlapPoint(worldPosition))
            {
                return true;
            }
        }

        return false;
    }

    private GrassEncounterEntry PickEncounter()
    {
        if (encounters == null || encounters.Count == 0)
        {
            return null;
        }

        float totalWeight = 0f;
        foreach (GrassEncounterEntry entry in encounters)
        {
            if (entry != null && entry.Species != null && entry.Weight > 0f)
            {
                totalWeight += entry.Weight;
            }
        }

        // An empty pool keeps legacy grass working with the Fight scene's default enemy.
        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = UnityEngine.Random.value * totalWeight;
        GrassEncounterEntry lastValidEntry = null;
        foreach (GrassEncounterEntry entry in encounters)
        {
            if (entry == null || entry.Species == null || entry.Weight <= 0f)
            {
                continue;
            }

            lastValidEntry = entry;
            roll -= entry.Weight;
            if (roll <= 0f)
            {
                return entry;
            }
        }

        return lastValidEntry;
    }
}
