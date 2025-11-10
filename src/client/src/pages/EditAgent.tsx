import { useState, useEffect } from "react"
import { useNavigate, useParams } from "react-router-dom"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { ArrowLeft, RefreshCw, Trash2 } from "lucide-react"
import { apiGet, apiPut, apiPost, apiDelete } from "@/lib/api"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog"

interface Agent {
  id: string
  hostname: string
}

export function EditAgent() {
  const navigate = useNavigate()
  const { id } = useParams()
  const [hostname, setHostname] = useState("")
  const [isLoading, setIsLoading] = useState(false)
  const [isLoadingData, setIsLoadingData] = useState(true)
  const [isValidating, setIsValidating] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [validationMessage, setValidationMessage] = useState<string | null>(null)

  useEffect(() => {
    const fetchAgent = async () => {
      if (!id) {
        setError("Agent ID is required")
        setIsLoadingData(false)
        return
      }

      setIsLoadingData(true)
      setError(null)

      try {
        const token = sessionStorage.getItem("token")
        if (!token) {
          navigate("/login")
          return
        }

        const agentData: Agent = await apiGet<Agent>(`/api/agent/${id}`)
        setHostname(agentData.hostname)
      } catch (err) {
        setError(err instanceof Error ? err.message : "An error occurred")
      } finally {
        setIsLoadingData(false)
      }
    }

    fetchAgent()
  }, [id, navigate])

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    setError(null)
    setIsLoading(true)

      try {
        const token = sessionStorage.getItem("token")
        if (!token) {
          navigate("/login")
          return
        }

        if (!id) {
          setError("Agent ID is required")
          setIsLoading(false)
          return
        }

        await apiPut(`/api/agent/${id}`, {
          hostname: hostname.trim(),
        })

      // Redirect back to agents list
      navigate("/agents")
    } catch (err) {
      if (err instanceof TypeError && err.message === "Failed to fetch") {
        setError("Unable to connect to the server. Please make sure the backend is running.")
      } else {
        setError(err instanceof Error ? err.message : "An error occurred")
      }
    } finally {
      setIsLoading(false)
    }
  }

  const handleRevalidate = async () => {
    if (!id) {
      setError("Agent ID is required")
      return
    }

    setIsValidating(true)
    setValidationMessage(null)
    setError(null)

    try {
      const token = sessionStorage.getItem("token")
      if (!token) {
        navigate("/login")
        return
      }

      const result = await apiPost<{ 
        message: string; 
        hostname: string; 
        pingUrl?: string; 
        response?: string;
        authenticated?: boolean;
        authResponse?: string;
      }>(
        `/api/agent/${id}/validate`
      )
      
      if (result.authenticated === true) {
        setValidationMessage(`✓ ${result.message}`)
        setError(null)
      } else if (result.authenticated === false) {
        setError(result.message)
        setValidationMessage(null)
      } else {
        // Agent is reachable but not authenticated (no token stored)
        setValidationMessage(result.message)
        setError(null)
      }
    } catch (err: any) {
      if (err instanceof TypeError && err.message === "Failed to fetch") {
        setError("Unable to connect to the server. Please make sure the backend is running.")
      } else {
        // Try to extract error message from response
        let errorMessage = "An error occurred during validation"
        if (err?.message) {
          errorMessage = err.message
        } else if (typeof err === 'string') {
          errorMessage = err
        }
        setError(errorMessage)
        setValidationMessage(null)
      }
    } finally {
      setIsValidating(false)
    }
  }

  const handleDelete = async () => {
    if (!id) {
      setError("Agent ID is required")
      return
    }

    try {
      const token = sessionStorage.getItem("token")
      if (!token) {
        navigate("/login")
        return
      }

      await apiDelete(`/api/agent/${id}`)
      // Redirect to agents list after deletion
      navigate("/agents")
    } catch (err) {
      if (err instanceof TypeError && err.message === "Failed to fetch") {
        setError("Unable to connect to the server. Please make sure the backend is running.")
      } else {
        setError(err instanceof Error ? err.message : "An error occurred while deleting the agent")
      }
    }
  }

  if (isLoadingData) {
    return (
      <div className="space-y-6">
        <div className="flex items-center gap-4">
          <Button variant="outline" onClick={() => navigate("/agents")}>
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back
          </Button>
        </div>
        <div className="rounded-lg border bg-card p-6 shadow-sm">
          <p className="text-muted-foreground">Loading agent...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Button variant="outline" onClick={() => navigate("/agents")}>
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back
          </Button>
          <div>
            <h1 className="text-3xl font-bold">Edit Agent</h1>
            <p className="text-muted-foreground mt-2">
              Update the agent hostname
            </p>
          </div>
        </div>
      </div>

      {error && (
        <div className="rounded-md bg-destructive/15 p-3 text-sm text-destructive">
          {error}
        </div>
      )}

      {validationMessage && (
        <div className="rounded-md bg-green-500/15 p-3 text-sm text-green-600 dark:text-green-400">
          {validationMessage}
        </div>
      )}

      <div className="rounded-lg border bg-card p-6 shadow-sm max-w-3xl">
        <form onSubmit={handleSubmit} className="space-y-6">
          <div className="space-y-2">
            <Label htmlFor="hostname">Hostname *</Label>
            <Input
              id="hostname"
              type="text"
              placeholder="agent.example.com"
              value={hostname}
              onChange={(e) => setHostname(e.target.value)}
              required
              disabled={isLoading}
            />
            <p className="text-sm text-muted-foreground">
              The hostname or address of the agent
            </p>
          </div>

              <div className="flex gap-4">
                <Button type="submit" disabled={isLoading || isValidating}>
                  {isLoading ? "Saving..." : "Save Changes"}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={handleRevalidate}
                  disabled={isLoading || isValidating}
                >
                  <RefreshCw className={`h-4 w-4 mr-2 ${isValidating ? "animate-spin" : ""}`} />
                  {isValidating ? "Validating..." : "Revalidate Connection"}
                </Button>
                <AlertDialog>
                  <AlertDialogTrigger asChild>
                    <Button
                      type="button"
                      variant="outline"
                      disabled={isLoading || isValidating}
                      className="text-destructive hover:text-destructive"
                    >
                      <Trash2 className="h-4 w-4 mr-2" />
                      Delete
                    </Button>
                  </AlertDialogTrigger>
                  <AlertDialogContent>
                    <AlertDialogHeader>
                      <AlertDialogTitle>Are you sure?</AlertDialogTitle>
                      <AlertDialogDescription>
                        This action cannot be undone. This will permanently delete the agent
                        <strong> {hostname}</strong> and all of its backup plans.
                      </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                      <AlertDialogCancel>Cancel</AlertDialogCancel>
                      <AlertDialogAction
                        onClick={handleDelete}
                        className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                      >
                        Delete
                      </AlertDialogAction>
                    </AlertDialogFooter>
                  </AlertDialogContent>
                </AlertDialog>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => navigate("/agents")}
                  disabled={isLoading || isValidating}
                >
                  Cancel
                </Button>
              </div>
        </form>
      </div>
    </div>
  )
}

