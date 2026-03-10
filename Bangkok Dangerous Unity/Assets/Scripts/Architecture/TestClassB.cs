using UnityEngine;

namespace GameArchitecture
{
    public class TestClassB : MonoBehaviour, IReference
    {
        private TestClassA TestClassA;

        public void Register()
        {
            ReferenceManager.AddReference_Self(gameObject, this);
        }

        public void Setup()
        {
            ReferenceManager.TryGetReference_Parent<TestClassA>(gameObject, out TestClassA);
        }

        public void Start()
        {
            Debug.Log(TestClassA.name);
        }
    }
}