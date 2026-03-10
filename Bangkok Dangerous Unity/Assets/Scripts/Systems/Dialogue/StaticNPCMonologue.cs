using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class StaticNPCMonologue : MonoBehaviour
{
    [Header("Monologue")]
    [Tooltip("If assigned, this TextMeshPro component provides the text and appearance per-prefab. If left empty the script will create one as a child.")]
    public TextMeshPro monologueTMP;

    [Header("Trigger")]
    [Tooltip("Distance from the player at which the NPC will start the monologue.")]
    public float triggerRadius = 3f;

    [Tooltip("If true the NPC will only play once. If false it can replay whenever the player re-enters range.")]
    public bool playOnce = true;

    [Header("Text positioning")]
    [Tooltip("Local height of the text above the NPC's pivot.")]
    public float textHeight = 2f;

    [Header("Timing")]
    [Tooltip("If > 0, text will remain visible for this many seconds when triggered. Accepts decimal values (e.g. 2.5). If <= 0, the audio clip length is used (or 3s if no clip).")]
    public float textDuration = 0f;

    [Header("Typewriter")]
    [Tooltip("Characters revealed per second. Set <= 0 to show full text instantly.")]
    public float lettersPerSecond = 40f;

    [Header("References (optional)")]
    [Tooltip("Assign your player transform here. If left empty, the script will try to find a GameObject tagged 'Player'.")]
    public Transform player;

    // runtime
    private AudioSource _audioSource;
    private bool _isPlaying;
    private bool _hasPlayed;

    // typing state
    private Coroutine _typingCoroutine;
    private bool _typingComplete;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 1f; // 3D sound
        _audioSource.loop = false;

        // Ensure a TextMeshPro (3D) is available. Prefer inspector assignment so each prefab can control font/outline/etc.
        if (monologueTMP == null)
        {
            var t = transform.Find("MonologueText");
            if (t != null)
                monologueTMP = t.GetComponent<TextMeshPro>();

            if (monologueTMP == null)
            {
                var go = new GameObject("MonologueText");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, textHeight, 0f);

                monologueTMP = go.AddComponent<TextMeshPro>();
                // sensible defaults — let the prefab override these in the inspector
                monologueTMP.alignment = TextAlignmentOptions.Center;
                monologueTMP.enableWordWrapping = false;
                monologueTMP.richText = true;
                monologueTMP.fontSize = 24f;
                monologueTMP.color = Color.white;
                go.SetActive(false);
            }
            else
            {
                t.localPosition = new Vector3(0f, textHeight, 0f);
                monologueTMP.gameObject.SetActive(false);
            }
        }
        else
        {
            // inspector-assigned TMP: ensure it's positioned under this transform for billboard positioning
            var parent = monologueTMP.transform;
            parent.localPosition = new Vector3(0f, textHeight, 0f);
            monologueTMP.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Lazy-find player by tag if not assigned
        if (player == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null) player = go.transform;
        }

        if (player == null) return;

        float dist = Vector3.Distance(player.position, transform.position);

        if (!_isPlaying && (!_hasPlayed || !playOnce) && dist <= triggerRadius)
        {
            StartMonologue();
        }

        // If the text is active, make it face the main camera (billboard)
        if (monologueTMP != null && monologueTMP.gameObject.activeSelf)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                monologueTMP.transform.rotation = Quaternion.LookRotation(monologueTMP.transform.position - cam.transform.position);
                monologueTMP.transform.localPosition = new Vector3(0f, textHeight, 0f);
            }
        }
    }

    private void StartMonologue()
    {
        // Use AudioSource.clip (assign per-prefab in the AudioSource component)
        if (_audioSource.clip == null && (monologueTMP == null || string.IsNullOrEmpty(monologueTMP.text)))
        {
            Debug.LogWarning($"StaticNPCMonologue on '{name}' has no audio clip assigned to its AudioSource and no text in the assigned TextMeshPro.");
            return;
        }

        if (_audioSource.clip != null)
            _audioSource.Play();

        if (monologueTMP != null)
        {
            // Use the text already set on the TextMeshPro component (per-prefab)
            // Start typewriter effect using monologueTMP.text
            if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
            _typingComplete = false;

            if (lettersPerSecond > 0f)
            {
                _typingCoroutine = StartCoroutine(TypeTextCoroutine());
            }
            else
            {
                monologueTMP.maxVisibleCharacters = int.MaxValue;
                _typingComplete = true;
            }

            monologueTMP.gameObject.SetActive(true);
            monologueTMP.ForceMeshUpdate(true, true);
        }

        _isPlaying = true;
        StartCoroutine(WaitForMonologueEnd());
    }

    private IEnumerator TypeTextCoroutine()
    {
        monologueTMP.ForceMeshUpdate(true);
        int totalChars = monologueTMP.textInfo.characterCount;

        if (totalChars == 0)
        {
            _typingComplete = true;
            yield break;
        }

        monologueTMP.maxVisibleCharacters = 0;

        if (lettersPerSecond <= 0f)
        {
            monologueTMP.maxVisibleCharacters = totalChars;
            _typingComplete = true;
            yield break;
        }

        float secondsPerChar = 1f / lettersPerSecond;
        float accumulator = 0f;
        int visible = 0;

        while (visible < totalChars)
        {
            accumulator += Time.deltaTime;
            int charsToShow = Mathf.FloorToInt(accumulator / secondsPerChar);
            if (charsToShow > 0)
            {
                visible += charsToShow;
                accumulator -= charsToShow * secondsPerChar;
                monologueTMP.maxVisibleCharacters = Mathf.Min(visible, totalChars);
            }
            yield return null;
        }

        monologueTMP.maxVisibleCharacters = totalChars;
        _typingComplete = true;
        _typingCoroutine = null;
    }

    private IEnumerator WaitForMonologueEnd()
    {
        if (textDuration > 0f)
        {
            yield return new WaitForSeconds(textDuration);
        }
        else if (_audioSource.clip != null)
        {
            while (_audioSource.isPlaying || !_typingComplete)
                yield return null;

            yield return new WaitForSeconds(0.1f);
        }
        else
        {
            while (!_typingComplete)
                yield return null;
            yield return new WaitForSeconds(3f);
        }

        if (monologueTMP != null)
        {
            monologueTMP.gameObject.SetActive(false);
            monologueTMP.maxVisibleCharacters = int.MaxValue;
        }

        _isPlaying = false;
        _hasPlayed = true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, triggerRadius);
    }
}