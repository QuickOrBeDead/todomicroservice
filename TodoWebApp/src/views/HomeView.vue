<template>
  <div class="home">
    <div class="container">
      <div class="row">
        <div class="col-12">
           <div class="d-flex flex-row mb-3 justify-content-center align-items-center">
            <div class="p-2"><img alt="logo" src="../assets/logo.png"></div>
            <div class="p-2"><h1>Todo App</h1></div>
          </div>
        </div>
      </div>
    </div>
    
    <div class="container text-center">
      <div class="row">
        <div class="col">
          <ul>
            <li v-for="task in tasks" :key="task.id" :class="task.completed ? 'checked' : ''"
              @click="changeCompleted(task)">
              {{ task.title }}
              <span class="close">×</span>
            </li>
          </ul>
        </div>
      </div>
    </div>


  </div>

</template>

<style>
ul li {
  cursor: pointer;
  position: relative;
  padding: 12px 8px 12px 40px;
  list-style-type: none;
  background: #eee;
  font-size: 18px;
  transition: 0.2s;
  
  -webkit-user-select: none;
  -moz-user-select: none;
  -ms-user-select: none;
  user-select: none;
}

ul li:nth-child(odd) {
  background: #f9f9f9;
}

ul li:hover {
  background: #ddd;
}

ul li.checked {
  background: #888;
  color: #fff;
  text-decoration: line-through;
}

ul li.checked::before {
  content: '';
  position: absolute;
  border-color: #fff;
  border-style: solid;
  border-width: 0 2px 2px 0;
  top: 10px;
  left: 16px;
  transform: rotate(45deg);
  height: 15px;
  width: 7px;
}

.close {
  position: absolute;
  right: 0;
  top: 0;
  padding: 12px 16px 12px 16px;
}

.close:hover {
  background-color: #f44336;
  color: white;
}
</style>

<script lang="ts">
import { Options, Vue } from 'vue-class-component'
import { createTaskManagementApi } from '@/api'
import { TaskApi, TaskListItemViewModel } from '@/metadata/task-management-api'

@Options({})
export default class HomeView extends Vue {
  taskApi: TaskApi = createTaskManagementApi(TaskApi)
  tasks: TaskListItemViewModel[] = []

  mounted(): void {
      this.listTasks()
  }

  listTasks(): void {
    this.taskApi.getTasks().then(x => this.tasks = x.data)
  }

  changeCompleted(task: TaskListItemViewModel): void {
    const completed = !task.completed;
    this.taskApi.changeCompleted(task.id as number, completed).then(() => task.completed = completed)
  }
}
</script>
