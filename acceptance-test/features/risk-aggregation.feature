Feature: Risk Aggregation

    # Idempotency
    Scenario: Risk message published twice
        Given a risk message was published
        When the same risk message is published
        Then risk is not duplicated