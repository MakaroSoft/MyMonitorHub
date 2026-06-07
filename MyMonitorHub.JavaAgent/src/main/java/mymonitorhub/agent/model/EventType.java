package mymonitorhub.agent.model;

public enum EventType {
    Unknown(0),
    Ok(1),
    Warn(2),
    Fail(3);

    private final int value;

    EventType(int value) {
        this.value = value;
    }

    public int getValue() {
        return value;
    }
}
