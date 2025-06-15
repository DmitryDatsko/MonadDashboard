from fastapi import FastAPI, HTTPException, Depends, Query
from fastapi.middleware.cors import CORSMiddleware
from pydantic_settings import BaseSettings
import hypersync
from hypersync import ClientConfig, Query as HQuery, JoinMode, FieldSelection, BlockField, TransactionField, TransactionSelection

class Settings(BaseSettings):
    rpc_url: str
    chunk_size: int = 5000
    max_transactions: int = 1_000_000

    class Config:
        env_file = ".env"

settings = Settings()

app = FastAPI(title="Hypersync tx count API", version="1.0.0")
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # lock down in production
    allow_credentials=True,
    allow_methods=["GET"],
    allow_headers=["*"],
)

# Create a single client and reuse it
client = hypersync.HypersyncClient(ClientConfig(url=settings.rpc_url))

@app.on_event("shutdown")
async def shutdown_hypersync():
    await client.aclose()

async def fetch_chunk(start: int, end: int, settings: Settings) -> tuple[int, bool]:
    q = HQuery(
        from_block=start,
        to_block=end,
        include_all_blocks=True,
        join_mode=JoinMode.JOIN_ALL,
        field_selection=FieldSelection(
            block=[BlockField.NUMBER],
            transaction=[TransactionField.HASH],
        ),
        transactions=[TransactionSelection()],
        max_num_blocks=settings.chunk_size,
        max_num_transactions=settings.max_transactions,
    )
    resp = await client.get(q)
    return len(resp.data.transactions), getattr(resp.data, "has_more", False)

@app.get("/count")
async def count_transactions(
    start: int = Query(..., ge=0),
    end:   int = Query(..., ge=0),
):
    if end < start:
        raise HTTPException(400, "Parameter `end` must be >= `start`")

    total = 0
    current = start
    try:
        while current <= end:
            count, has_more = await fetch_chunk(current, end, settings)
            total += count
            if not has_more:
                break
            current += settings.chunk_size
    except hypersync.HypersyncError as e:
        raise HTTPException(502, f"Hypersync error: {e}")
    except Exception as e:
        raise HTTPException(500, f"Unexpected error: {e}")

    return {"start": start, "end": end, "transactions": total}
