from fastapi import FastAPI, HTTPException, Query as QueryParam
from fastapi.middleware.cors import CORSMiddleware
import hypersync
from hypersync import (
    BlockField, TransactionField, JoinMode,
    ClientConfig, TransactionSelection, FieldSelection, Query
)

app = FastAPI(
    title="Hypersync tx count API",
    version="1.0.0"
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["GET"],
    allow_headers=["*"],
)

RPC_URL="https://monad-testnet.hypersync.xyz"
CHUNK_SIZE= 5000

async def fetch_chunk(start: int, end: int) -> int:
    client = hypersync.HypersyncClient(ClientConfig(
        url="https://monad-testnet.hypersync.xyz"
    ))

    q = Query(
        from_block=start,
        to_block=end,
        include_all_blocks=True,
        join_mode=JoinMode.JOIN_ALL,
        field_selection=FieldSelection(
            block=[BlockField.NUMBER],
            transaction=[TransactionField.HASH]
        ),
        transactions=[TransactionSelection()],
        max_num_blocks=CHUNK_SIZE,
        max_num_transactions=1_000_000
    )

    response = await client.get(q)

    return len(response.data.transactions)

@app.get("/count")
async def count_transactions(
    start: int = QueryParam(..., ge=0, description="Start block number"),
    end: int = QueryParam(..., ge=0, description="End block number"),
):
    if end < start:
        raise HTTPException(status_code=400, detail="Parameter end must be >= start")
    # if (end - start) > CHUNK_SIZE:
    #     raise HTTPException(status_code=400, detail="Max chunck size 5000")
    
    try:
        tx_count = await fetch_chunk(start, end)
    except Exception as e:
        raise HTTPException(status_code=502, detail=f"Error: {e}")
    
    return {"start": start, "end": end, "transactions": tx_count}
