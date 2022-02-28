export interface TableDefinition<Type = any> {
    /** Returns the table as a 2-D array. */
    raw(): string[][];

    /** Returns the table as a 2-D array, without the first row. */
    rows(): string[][];

    /** Returns an object where each row corresponds to an entry (first column is the key, second column is the value). */
    rowsHash(): { [columnName in keyof Type]: string };

    /** Returns an array of objects where each row is converted to an object (column header is the key). */
    hashes(): Array<{ [columnName in keyof Type]: string }>;
}