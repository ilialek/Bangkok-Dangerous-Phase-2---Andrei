namespace GameArchitecture
{
    public enum ScopeType
    {
        Global,     //Global instance in game
        Scene,      //Instance per scene
        Self,       //Instance on gameObject
        Parent,     //Searches for scope in parent, upwards
        Children,   //Searches for scope in children, downwards
    }
}