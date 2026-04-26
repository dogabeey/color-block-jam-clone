namespace Game
{


    public enum GameEvent
    {
        NONE = 0,
        LEVEL_COMPLETED = 100,
        LEVEL_FAILED = 101,
        LEVEL_STARTED = 102,

        GRID_INITIALIZED = 200,
        BLOCK_CLEARED = 201,
        BOARD_CLEARED = 202,
        
        LEVEL_TIMER_TICK = 303,
        LEVEL_TIMER_EXPIRE = 304,
    }
}