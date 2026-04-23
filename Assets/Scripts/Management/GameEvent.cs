namespace Game.Management
{


    public enum GameEvent
    {
        NONE = 0,

        OBJECTIVES_INITIALIZED = 100,
        OBJECTIVE_COMPLETED = 101,
        OBJECTIVE_FAILED = 102,

        LEVEL_COMPLETED = 200,
        LEVEL_FAILED = 201,
        LEVEL_STARTED = 202,

        CURRENT_WORLD_CHANGED = 300,

        CAMERA_BOUNDS_UPDATED = 400,
        ELEMENT_SELECTED = 401,
        ELEMENT_MATCHED = 402,
        ELEMENT_DESTROYED = 403,
        ELEMENTS_SWAPPED = 404,
        SWAP_FAILED = 405,
        MATCH_DETECTED = 406,
        COMBO_TRIGGERED = 407,
        GRAVITY_STARTED = 408,
        GRAVITY_COMPLETED = 409,
        ELEMENTS_REFILLED = 410,
        GRID_INITIALIZED = 411,
        GRID_STABLE = 412,
        HIGHLIGHT_UPDATED = 413,
        NO_POSSIBLE_MOVES = 414,

        // Currency Events
        CURRENCY_EARNED = 500,
        CURRENCY_SPENT = 501,
        CURRENCY_CHANGED = 502,

        // Action Bar Events
        ACTION_BAR_ITEM_CLICKED = 600,
        ACTION_SUCCESSFUL = 601,
        ACTION_FAILED = 602,

        // UI Events
        SCREEN_OPENED = 700,
        SCREEN_CLOSED = 701,
        LOADING_SCREEN_COMPLETE = 702,

        // Special Elements
        BOX_DESTROYED = 800,
        BREAKABLE_WALL_DESTROYED = 801,
        SPECIAL_ELEMENT_ACTIVATED = 802,
        BOMB_CREATED = 803,
        ROCKET_CREATED = 804,
        DISCO_BALL_CREATED = 805,

        // Ad Events
        AD_SHOWN = 900,
        AD_CLOSED = 901,
        AD_FAILED = 902,
        REWARDED_AD_SHOWN = 903,
        REWARDED_AD_COMPLETED = 904,
        REWARDED_AD_FAILED = 905,

        // Sound Events
        SOUND_PLAYED = 1000,
        MUSIC_STARTED = 1001,
        MUSIC_STOPPED = 1002,
        SOUND_SETTINGS_CHANGED = 1003,

        // Timer Events
        TIMER_STARTED = 1100,
        TIMER_EXPIRED = 1101,
        TIMER_PASSED = 1102,

        // Input events
        INPUT_RECEIVED = 1200,
    }
}