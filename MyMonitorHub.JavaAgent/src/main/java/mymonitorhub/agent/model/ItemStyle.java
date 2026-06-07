package mymonitorhub.agent.model;

public enum ItemStyle {
    SendsHealth(1),
    NoHealth(2);

    private final int value;

    ItemStyle(int value) {
        this.value = value;
    }

    public int getValue() {
        return value;
    }
}
