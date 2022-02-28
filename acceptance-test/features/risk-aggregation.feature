Feature: Risk Aggregation

    @idempotency @cleanDatabase
    Scenario: Risk message published twice
        Given a risk message was published
        When the same risk message is published
        Then risk is not duplicated

    @aggregation @cleanDatabase
    Scenario: Multiple risk message hierarchy
        Given the following risk messages
            | Amount | Hierarchy           |
            | 101    | Delta:FXSpot:EMEA   |
            | 102    | Delta:FXSpot:APAC   |
            | 103    | Delta:FXSpot:AMER   |
            | 104    | Delta:FXOption:EMEA |
        When risk messages are published
        Then the following risk aggregations are expected
            | Identifier          | Amount | 
            | Delta:FXSpot:EMEA   | 101    | 
            | Delta:FXSpot:APAC   | 102    | 
            | Delta:FXSpot:AMER   | 103    | 
            | Delta:FXOption:EMEA | 104    |